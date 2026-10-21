<h1 align="center">
    GameSharp.Collections<br/>
    <a href="https://www.nuget.org/packages/GameSharp.Collections"><img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/GameSharp.Collections"></a>
    <a href="https://www.nuget.org/packages/GameSharp.Collections"><img alt="NuGet Version" src="https://img.shields.io/nuget/v/GameSharp.Collections"></a>
    <a href="https://ko-fi.com/O5T4273KRI"><img alt="Buy Me a Coffee at ko-fi.com" src="https://img.shields.io/badge/ko--fi-buy_me_a_coffee-FF6433?logo=kofi">
</a>
</h1>
An aggressively optimised, Native AOT-compatible collection library designed for game engines, data-driven architectures, and high-performance applications in .NET 10.

## Quick Start

Install [the NuGet package](https://www.nuget.org/packages/GameSharp.Collections):

```bash
dotnet add package GameSharp.Collections
```

### Singlethreaded Type Lookups

Use `TypeLookup` to build blazing-fast service locators or component containers. Retrieval is hardware accelerated, and querying derived types or interfaces allocates zero memory.

```csharp
using GameSharp.Collections;

TypeLookup components = new TypeLookup();

// Add instances
components.Add(new Transform());
components.Add(new PhysicsBody());
components.Add(new PlayerController());

// Retrieve a single Transform component
if (components.TryGetOne(out Transform? transform))
{
    transform.Position += new Vector3(0, 1, 0);
}

// Replace an existing instance in-place
components.Replace(transform, new Transform());

// Retrieves PlayerController and anything else implementing IUpdatable
foreach (IUpdatable updatable in components.GetAll<IUpdatable>())
{
    updatable.Update(deltaTime);
}

```

### Multithreaded Type Lookups

For multithreaded environments, choose between `ConcurrentTypeLookup` (hybrid lock-to-lock-free lifecycle) or `ImmutableTypeLookup` (pure lock-free Copy-On-Write).

#### Option 1: `ConcurrentTypeLookup`

Synchronizes mutations with a reader-writer lock during initialization, then transitions via `Publish()` to a lock-free, snapshot-based runtime state:

```csharp
using GameSharp.Collections.Concurrent;

using ConcurrentTypeLookup services = new ConcurrentTypeLookup();

// Thread-safe synchronized mutations during setup
services.Add(new AudioService());
services.Add(new RenderSystem());

// Transition to published runtime state (completes in-flight locks and switches to lock-free COW updates)
services.Publish();

// Subsequent reads and updates are lock-free
services.Add(new NetworkService());
```

#### Option 2: `ImmutableTypeLookup`

Batch your additions using the `Builder` to prevent intermediate allocations, then seal it for lock-free concurrent reading:

```csharp
using GameSharp.Collections.Immutable;

// Batch initialization without allocating intermediate lookups
ImmutableTypeLookup.Builder builder = ImmutableTypeLookup.CreateBuilder();
builder.Add(new AudioService());
builder.Add(new RenderSystem());

// Seal it for thread-safe access
ImmutableTypeLookup services = builder.ToImmutable();

// Safely swap state atomically across threads using the Copy-On-Write (COW) pattern
ImmutableTypeLookup.InterlockedAdd(ref services, new NetworkService());
```

### Resizable Rented Arrays

`PooledList<T>` acts as a zero-allocation, drop-in replacement for `List<T>`, using arrays leased from a shared pool.

> [!CAUTION]
> Mutating the list while holding a `Span<T>` or enumerating it can return the underlying array to the pool, leaving you pointing to corrupted memory.

```csharp
using GameSharp.Collections;

void ProcessData()
{
    // The underlying array is guaranteed to return to the pool when the method exits.
    using PooledList<Vector3> positions = PooledList.Empty<Vector3>(initialCapacity: 32);

    for (int i = 0; i < 100; i++)
    {
        positions.Add(new Vector3(i, 0, 0)); // Auto-grows dynamically
    }

    // Pass as Span<T> or ReadOnlySpan<T> for safe, read/write access
    UpdatePositions(positions);

    // Always pass the list itself by 'ref' or 'scoped ref' to mutate the collection downstream
    AddMorePositions(ref positions);
}

void UpdatePositions(Span<Vector3> span) { /* ... */ }
void AddMorePositions(scoped ref PooledList<Vector3> list) { /* ... */ }
```

You can also collect and await `Task` instances without allocating task arrays using `PooledList.WhenAll`:

```csharp
await PooledList.WhenAll((ref PooledList<Task> tasks, CancellationToken ct) =>
{
    tasks.Add(LoadAssetsAsync(ct));
    tasks.Add(InitializeAudioAsync(ct));
}, cancellationToken);
```

## Architecture & Classes

### ReadOnlyTypeLookup & Lookup Interfaces (`IReadOnlyTypeLookup`, `ITypeLookup`)

At its core, this library is an attempt to marry two worlds that don't usually mix: the **Data-Oriented Design (DOD)** of a pure ECS, and the **Object-Oriented Design (OOD)** of C#. Unlike a "true" ECS where components are raw structs packed into contiguous arrays, components here are ordinary C# reference types. The trade-off is that the component *data itself* is scattered across the managed heap rather than laid out contiguously, a deliberate concession to usability and thread-safety.

The result is a highly flexible architecture ideal for projects where absolute cache locality is not a strict requirement, heavily dependent on polymorphism, and operating in multithreaded environments where single-instruction reference updates prevent the memory tearing issues that can arise from oversized structs.

All lookup implementations inherit from `ReadOnlyTypeLookup` (and `MutableTypeLookup` for mutable variants) and implement `IReadOnlyTypeLookup` or `ITypeLookup` for polymorphic collection access.

#### Features

* **Zero-allocation $\mathcal{O}(D \log T)$ Polymorphic Queries**: Where $D$ is the number of derived types implementing the queried interface, and $T$ is the total number of unique types.
* **Zero-allocation $\mathcal{O}(\log T)$ Exact Type Queries**: Where $T$ is the total number of unique types stored in the collection.
* **LINQ & Zero-Allocation Enumeration**: Concrete `Query` and `Query<T>` structs implement `IEnumerable<T>` for LINQ compatibility while providing stack-only `ref struct` enumerators for zero-allocation `foreach` loops.
* **Dynamic Runtime Unloading**: Unloaded assemblies are automatically purged from The Directed Acyclic Graph (DAG).

> [!IMPORTANT]
> You must ensure that all instances of a type are removed from the lookup before unloading its assembly context. Otherwise, the lookup will retain stale references to types that no longer exist.

### TypeLookup

The `TypeLookup` is the primary single-threaded mutable implementation of `MutableTypeLookup` and `ITypeLookup`. It provides the ability to dynamically add, remove, replace, and clear objects while maintaining the strictly ordered, contiguous internal arrays.

#### Features

* **$\mathcal{O}(N)$ Insertion & Removal**: Keeps internal lookup tables tightly packed and sorted by precomputed Directed Acyclic Graph (DAG) IDs.
* **In-Place Replacement**: Supports replacing existing instances via `Replace` without rebuilding lookup cluster indices.
* **Dynamic Resizing & Mutation Monitoring**: Automatically manages internal array capacities as objects of varying types are added, and guards enumerators against concurrent modification during iteration.

### ConcurrentTypeLookup

The `ConcurrentTypeLookup` is a mutable, thread-safe implementation of `MutableTypeLookup` and `ITypeLookup`. It is designed for scenarios where a collection is populated or modified across multiple threads during setup and later accessed heavily during runtime.

#### Features

* **Two-Phase Lifecycle (`Publish`)**: Uses `CountingReaderWriterLock` for synchronized in-place mutations prior to publishing. Calling `Publish()` waits for active locks to drain, closes the lock, and switches all subsequent mutations (`Add`, `Remove`, `Replace`, `Clear`) to a lock-free, snapshot-based `Interlocked` path.
* **Lock-Guarded or Lock-Free Enumeration**: Automatically acquires a scoped read lock handle during enumeration prior to publishing, or reads directly from the immutable snapshot once published.

### ImmutableTypeLookup

The `ImmutableTypeLookup` is a thread-safe, immutable variant of the lookup collection implementing `IReadOnlyTypeLookup` and `ICloneable`. It is ideal for read-heavy, highly concurrent environments where multiple threads (such as game loop worker threads) need to query components or services simultaneously.

#### Features

* **Lock-Free Thread Safety**: Because the underlying data cannot change, it is inherently thread-safe for concurrent reads.
* **Atomic Updates**: Provides built-in `InterlockedAdd`, `InterlockedRemove`, and `InterlockedReplace` methods, as well as `InterlockedUpdate<T>` accepting custom `IImmutableTypeLookupTransformer` structs for zero-allocation optimistic concurrency using the Copy-On-Write (COW) pattern.
* **Seamless Conversion**: Easily converts to and from `TypeLookup`, `ConcurrentTypeLookup`, and `ImmutableTypeLookup.Builder` instances via extension methods.

### ImmutableTypeLookup.Builder

The `ImmutableTypeLookup.Builder` is a mutable companion to the immutable lookup. It allows you to perform multiple additions, removals, and replacements in batches before finally "sealing" the collection into an `ImmutableTypeLookup`. This prevents the allocations and overhead of creating intermediate immutable copies during heavy initialization phases.

#### Features

* **Allocation-Free Batching**: Add or remove dozens of components at once using mutable array builders under the hood.
* **One-Way Sealing**: Once `ToImmutable()` is called, the builder locks itself from further modifications, safely handing off its internal state to the immutable instance.

### PooledList

Manual array pooling is error-prone: developers must handle resizing, purge stale references, and guarantee buffer disposal during exceptions. Wrapping this boilerplate inside heap-allocated classes adds garbage collection pressure to hot paths, defeating the purpose of pooling.

Established solutions like [Collections.Pooled.PooledList](https://www.google.com/search?q=https://github.com/jtmueller/Collections.Pooled/blob/master/Collections.Pooled/PooledList.cs) or the Community Toolkit's [ArrayPoolBufferWriter](https://github.com/CommunityToolkit/dotnet/blob/main/src/CommunityToolkit.HighPerformance/Buffers/ArrayPoolBufferWriter%7BT%7D.cs) resort to a class because standard C# structs copy by value. Passing a value type across method boundaries creates shallow copies, making stateful operations like dynamic resizing hazardous if a detached copy retains an invalidated buffer reference or outlives disposal. A ref struct bridges this divide: strictly confined to the stack, it prevents boxing, heap escape, and multi-threaded data races by design.

By using a ref struct, `PooledList<T>` strikes a practical balance between safety and performance. It wraps `ArrayPool<T>` with the ergonomics of a standard list, handling growth, reference clearing, and pool returns automatically at zero GC cost. While callers must still pass the instance by reference to prevent stack copies from desynchronising during resizes, it eliminates heap allocations entirely without the maintenance burden of raw buffer management.

#### Features

* **Guaranteed Cleanup**: Allows you to use the `using` syntax to return the array to the pool.
* **Pooled Task Awaiting (`WhenAll`)**: Provides `PooledList.WhenAll` overloads and `CollectorDelegate` to gather and await asynchronous tasks using pooled buffers that automatically return to the pool upon task completion.
* **Native Span Integration**: Implicitly converts to `Span<T>` and `ReadOnlySpan<T>` for seamless interoperability with high-performance .NET APIs.
* **Multiplexing**: Intelligently routes array renting to shared pools based on type characteristics (distinguishing between value types, reference types, and raw bytes), reducing pool fragmentation in AOT environments.

### CountingReaderWriterLock

A disposable reader-writer synchronization primitive that tracks active lock holders and blocks closure or disposal until all active operations have released their locks.

#### Features

* **Zero-Allocation Lock Handles**: Returns disposable `ref struct` handles (`ReadLockHandle`, `WriteLockHandle`, `UpgradableReadLockHandle`, and `UpgradedReadLockHandle`) for structured, exception-safe lock management.
* **Graceful Closure (`Close`)**: Prevents new lock acquisitions while waiting for all in-flight operations to complete, enabling safe transitions from synchronized mutation to lock-free runtime states.

## License

This project is licensed under the **PolyForm Perimeter License 1.0.0**.

> **Summary:** You are free to use, modify, and distribute this software, provided you do not use it to build a product that competes with the software itself.

Please see the [LICENSE.md](https://www.google.com/search?q=https://github.com/gamesharp/Core/blob/master/LICENSE.md) file for the full legal text.