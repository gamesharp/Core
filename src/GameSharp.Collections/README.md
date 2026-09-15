# GameSharp.Collections

An aggressively optimized, AOT-compatible, cache-friendly C# collection library targeting .NET 10. `GameSharp.Collections` provides highly efficient object lookup and retrieval by type, making it ideal for high-performance scenarios such as game engines, Entity-Component Systems (ECS), and heavy data-processing applications.

## Key Features

* **O(log N) / Cache-Optimized Lookups:** Employs a custom `HybridSearch` algorithm that balances binary and linear searching, tuned perfectly to your CPU's L1 cache line size (via automatic OS-level detection on Windows, macOS, and Linux).
* **Hardware Acceleration:** Utilizes SIMD/Vector instructions for extremely fast offset calculations and cluster lookups during collection mutations.
* **AOT Compatible:** Fully supports Native AOT, avoiding dynamic code generation where possible while maintaining flexibility.
* **Polymorphic Queries:** Rapidly retrieve objects by their exact type, base type, or implemented interface using `GetOne<T>()` and `GetAll<T>()`.
* **Mutable & Immutable Variants:** 
  * `TypeLookup`: A mutable, highly performant collection of items grouped by type.
  * `ImmutableTypeLookup`: A thread-safe, immutable counterpart with robust `Builder` and lock-free `InterlockedUpdate` support.
* **Zero-Allocation Enumeration:** Iteration over type clusters is allocation-free using custom `ref struct` enumerators.
* **`PooledList<T>`:** A high-performance `ref struct` list backed by `ArrayPool`, designed to minimize allocations and improve memory locality for both value and reference types.

## Performance & Benchmarks

Performance is the primary goal of this library. The layout of metadata (`IntegerLookup`) and object instances are strictly decoupled to maximize cache locality. 

![Benchmark Graph](https://raw.githubusercontent.com/gamesharp/Core/master/media/typelookup_v1_benchmarks_graph.png)

## Installation

Add the library to your .NET 10 project:

```xml
<ItemGroup>
  <PackageReference Include="GameSharp.Collections" Version="2.0.0" />
</ItemGroup>
```

## Usage

### Mutable `TypeLookup`

`TypeLookup` allows you to store mixed types and query them instantly.

```csharp
using GameSharp.Collections;

// 1. Initialize the collection
var lookup = new TypeLookup();

// 2. Add items
lookup.Add(new Player { Name = "Hero" });
lookup.Add(new Enemy { Name = "Goblin" });
lookup.Add(new Transform { X = 10, Y = 20 });

// 3. Fast retrieval (Single item)
if (lookup.TryGetOne(out Player p))
{
    Console.WriteLine($"Found player: {p.Name}");
}

// 4. Fast retrieval (Multiple items)
// GetAll<T>() returns an allocation-free query struct.
foreach (var enemy in lookup.GetAll<Enemy>())
{
    Console.WriteLine($"Enemy: {enemy.Name}");
}
```

### Immutable Collections & Builders

For thread-safe or functional architectures, use `ImmutableTypeLookup`.

```csharp
using GameSharp.Collections.Immutable;

// Use a builder for efficient batch initialization
var builder = ImmutableTypeLookup.CreateBuilder();
builder.Add(new Player());
builder.Add(new Weapon());

// Finalize into an immutable lookup
ImmutableTypeLookup immutableLookup = builder.ToImmutable();

// Or convert a mutable TypeLookup to an ImmutableTypeLookup
ImmutableTypeLookup snapshot = myMutableLookup.AsImmutableTypeLookup();
```

### Atomic Updates (InterlockedUpdate)

`ImmutableTypeLookup` supports lock-free mutations via `InterlockedUpdate`, which safely replaces the collection instance in highly concurrent environments:

```csharp
ImmutableTypeLookup.InterlockedUpdate(
    ref mySharedLookup, 
    (current, itemToAdd) => current.Add(itemToAdd), 
    new Weapon()
);
```

### `PooledList<T>`

`PooledList<T>` is a pooled, disposable list optimized for low-allocation hot paths.
```csharp
using GameSharp.Collections;

PooledList<int> list = new(initialCapacity: 8);
try
{
    list.Add(10); 
    list.Add(20); 
    list.Insert(15, index: 1);
    bool has20 = list.Contains(20);
    int indexOf15 = list.IndexOf(15);
    list.Remove(10); 
    list.RemoveAt(0);
    foreach (int item in list) { Console.WriteLine(item); }
}
finally
{
    list.Dispose(); // Return the internal array to the pool
}
```

## License

This project is licensed under the **PolyForm Perimeter License 1.0.0**.

> **Summary:** You are free to use, modify, and distribute this software, provided you do not use it to build a product that competes with the software itself.

Please see the [LICENSE.md](https://github.com/gamesharp/Core/blob/master/LICENSE.md) file for the full legal text.
