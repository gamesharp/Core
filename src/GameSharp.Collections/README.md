<h1 align="center">
    GameSharp.Collections<br/>
    <a href="https://www.nuget.org/packages/GameSharp.Collections"><img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/GameSharp.Collections"></a>
    <a href="https://www.nuget.org/packages/GameSharp.Collections"><img alt="NuGet Version" src="https://img.shields.io/nuget/v/GameSharp.Collections"></a>
    <a href="https://ko-fi.com/O5T4273KRI"><img alt="Buy Me a Coffee at ko-fi.com" src="https://img.shields.io/badge/ko--fi-buy_me_a_coffee-FF6433?logo=kofi">
</a>
</h1>
An aggressively optimised, Native AOT-compatible collection library designed for game engines, data-driven architectures, and high-performance applications in .NET 10.

## Quick Start

Install the NuGet package:

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

// Retrieves PlayerController and anything else implementing IUpdatable
foreach (IUpdatable updatable in components.GetAll<IUpdatable>())
{
    updatable.Update(deltaTime);
}
```

### Multithreaded Type Lookups

For multithreaded environments, use `ImmutableTypeLookup`. Batch your additions using the `Builder` to prevent intermediate allocations, then seal it for lock-free concurrent reading.

```csharp
using GameSharp.Collections.Immutable;

// Batch initialization without allocating intermediate lookups
ImmutableTypeLookup.Builder builder = ImmutableTypeLookup.CreateBuilder();
builder.Add(new AudioService());
builder.Add(new RenderSystem());

// Seal it for thread-safe access
ImmutableTypeLookup services = builder.ToImmutable();

// Safely swap state atomically across threads using the Copy-On-Write (COW) pattern
ImmutableTypeLookup.InterlockedUpdate(ref services, lookup => lookup.Add(new NetworkService()));
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

## Classes

### ReadOnlyTypeLookup

At its core, this library is an attempt to marry two worlds that don't usually mix: the **Data-Oriented Design (DOD)** of a pure ECS, and the **Object-Oriented Design (OOD)** of C#. Unlike a "true" ECS where components are raw structs packed into contiguous arrays, components here are ordinary C# reference types. The trade-off is that the component _data itself_ is scattered across the managed heap rather than laid out contiguously, a deliberate concession to usability and thread-safety.

The result is a highly flexible architecture ideal for projects where absolute cache locality is not a strict requirement, heavily dependent on polymorphism, and operating in multithreaded environments where single-instruction reference updates prevent the memory tearing issues that can arise from oversized structs.

![Benchmark Graph](https://raw.githubusercontent.com/gamesharp/Core/master/media/typelookup_v1_benchmarks_graph.png)

#### Features

- **Zero-allocation $\mathcal{O}(D \log T)$ Polymorphic Queries**: Where $D$ is the number of derived types implementing the queried interface, and $T$ is the total number of unique types.
- **Zero-allocation $\mathcal{O}(\log T)$ Exact Type Queries**: Where $T$ is the total number of unique types stored in the collection.
- **Dynamic Runtime Unloading**: Unloaded assemblies are automatically purged from The Directed Acyclic Graph (DAG).
  > [!IMPORTANT]
  > You must ensure that all instances of a type are removed from the lookup before unloading its assembly context. Otherwise, the lookup will retain stale references to types that no longer exist.

### TypeLookup

The `TypeLookup` is the primary mutable implementation of `ReadOnlyTypeLookup`. It provides the ability to dynamically add, remove, and clear objects while maintaining the strictly ordered, contiguous internal arrays required by the SIMD search algorithms.

#### Features

- **$\mathcal{O}(N)$ Insertion & Removal**: Keeps internal lookup tables tightly packed and sorted by precomputed Directed Acyclic Graph (DAG) IDs for blazing-fast reads.

- **Version Tracking**: Includes built-in mutation monitors that will safely throw an `InvalidOperationException` if the collection is modified while being enumerated.

- **Dynamic Resizing**: Automatically manages internal array capacities as objects of varying types are added.

### ImmutableTypeLookup

The `ImmutableTypeLookup` is a thread-safe, immutable variant of the lookup collection. It is ideal for read-heavy, highly concurrent environments where multiple threads (such as game loop worker threads) need to query components or services simultaneously.

#### Features

- **Lock-Free Thread Safety**: Because the underlying data cannot change, it is inherently thread-safe for concurrent reads.

- **Atomic Updates**: Provides `InterlockedUpdate` methods to safely and atomically swap the immutable state in a multithreaded context without traditional locks using the Copy-On-Write (COW) pattern.

- **Seamless Conversion**: Easily converts to and from mutable `TypeLookup` instances.

#### Builder

The `ImmutableTypeLookup.Builder` is a mutable companion to the immutable lookup. It allows you to perform multiple additions and removals in batches before finally "sealing" the collection into an `ImmutableTypeLookup`. This prevents the allocations and overhead of creating intermediate immutable copies during heavy initialization phases.

#### Features

- **Allocation-Free Batching**: Add or remove dozens of components at once using mutable array builders under the hood.

- **One-Way Sealing**: Once `ToImmutable()` is called, the builder locks itself from further modifications, safely handing off its internal state to the immutable instance.

### PooledList

Manual array pooling is notoriously error-prone. Developers must handle resizing, clear stale references to prevent memory leaks, and guarantee arrays are returned even during exceptions. Adding this boilerplate to hot paths often negates the benefits of pooling entirely.

`PooledList<T>` solves this by acting as a lightweight `ref struct` wrapper around `ArrayPool<T>`. It provides a familiar `List<T>` abstraction that handles dynamic growth, clearing unused references and returning arrays back to the pool. When exclusively passed by reference, the risk of desynchronised state and stale memory references is fully mitigated.

#### Features

- **Guaranteed Cleanup**: Allows you to use the `using` syntax to return the array to the pool.

- **Native Span Integration**: Implicitly converts to `Span<T>` and `ReadOnlySpan<T>` for seamless interoperability with high-performance .NET APIs.

- **Multiplexing**: Intelligently routes array renting to shared pools based on type characteristics (distinguishing between value types, reference types, and raw bytes), reducing pool fragmentation in AOT environments.

## License

This project is licensed under the **PolyForm Perimeter License 1.0.0**.

> **Summary:** You are free to use, modify, and distribute this software, provided you do not use it to build a product that competes with the software itself.

Please see the [LICENSE.md](https://github.com/gamesharp/Core/blob/master/LICENSE.md) file for the full legal text.
