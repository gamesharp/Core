<h1 align="center">
    GameSharp.Random<br/>
    <a href="https://www.nuget.org/packages/GameSharp.Random"><img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/GameSharp.Random"></a>
    <a href="https://www.nuget.org/packages/GameSharp.Random"><img alt="NuGet Version" src="https://img.shields.io/nuget/v/GameSharp.Random"></a>
    <a href="https://ko-fi.com/O5T4273KRI"><img alt="Buy Me a Coffee at ko-fi.com" src="https://img.shields.io/badge/ko--fi-buy_me_a_coffee-FF6433?logo=kofi"></a>
</h1>

A high-performance, thread-safe pseudo-random number generator (PRNG) library built for .NET 10. Designed primarily for games, simulations, and procedural generation where low allocation, deterministic control, and geometric math support are critical.

## Features

- **Geometric & Spatial Randomness:**
  - Normalized direction vectors: `NextUnitVector2()`, `NextUnitVector3()`, `NextUnitVector4()`
  - 2D circular boundary points: `NextUnitCircleVector2(radius)`
  - 3D rotations: `NextRotation()` generating random yaw/pitch/roll `Quaternion` instances
- **Distribution Profiles:**
  - Uniform ranges for primitive floating point and integer types
  - Box-Muller Gaussian / Normal distribution: `NextGaussian(mean, standardDeviation)`
  - Curvature biases: `MinBiasedRange`, `MaxBiasedRange`, `MedianBiasedRange`, and `ExtremesBiasedRange`
- **Collection Operations:**
  - High-performance Fisher-Yates `Shuffle` for `Span<T>` and `IList<T>`
  - Uniform `Choose<T>` over spans, lists, or slice windows (`offset`, `length`)
  - Random Enum selection: `NextEnum<T>()`
- **Probabilistic Conditions:**
  - Declarative conditions using predefined `Chance` weights (`Impossible`, `Remote`, `Unlikely`, `Even`, `Likely`, `Expected`, `Certain`)
- **Explicit Seed Architecture:**
  - 8-byte value type (`Seed`) implementing `ISpanFormattable`, `IUtf8SpanFormattable`, and `ISpanParsable<Seed>`
  - Deterministic replay by passing `ref Seed` or `Seed` explicitly

## Quick Start

Install [the NuGet package](https://www.nuget.org/packages/GameSharp.Random):

```bash
dotnet add package GameSharp.Random
```

### 1. Basic Generation (Thread-Local)

By default, `Randomizer` methods use an isolated `[ThreadStatic]` seed, eliminating thread locks:

```csharp
using GameSharp.Random;

// Primitive types
int roll = Randomizer.UniformRange(min: 1, max: 7);     // 1 to 6
float fraction = Randomizer.NextFloat();                // [0.0f, 1.0f)
byte b = Randomizer.NextByte();

// Percentage-based conditions
if (Randomizer.Condition(Chance.Likely)) // 75% probability
{
    Console.WriteLine("Condition met!");
}
```

### 2. Vectors, Geometry & Rotations

Generate normalized vectors and random orientations directly compatible with `System.Numerics`:

```csharp
using System.Numerics;
using GameSharp.Random;

// Unit vectors (normalized to length 1.0)
Vector2 dir2D = Randomizer.NextUnitVector2();
Vector3 dir3D = Randomizer.NextUnitVector3();
Vector4 dir4D = Randomizer.NextUnitVector4();

// Point along the perimeter of a circle (radius 5.0)
Vector2 pointOnCircle = Randomizer.NextUnitCircleVector2(radius: 5.0f);

// 3D rotation quaternion
Quaternion orientation = Randomizer.NextRotation();
```

### 3. Biased Ranges and Distributions

Control value spread beyond simple uniform distribution:

```csharp
// Gaussian (Normal) Distribution
float heightVariation = Randomizer.NextGaussian(mean: 175.0f, standardDeviation: 10.0f);

// Biased ranges (e.g. loot drop qualities or terrain falloff)
float lowLoot = Randomizer.MinBiasedRange(1.0f, 100.0f);        // Weighted towards min
float highThreat = Randomizer.MaxBiasedRange(1.0f, 10.0f);      // Weighted towards max
float averageStat = Randomizer.MedianBiasedRange(10.0f, 20.0f); // Weighted towards center
float volatileValue = Randomizer.ExtremesBiasedRange(0f, 1f);   // Weighted towards 0 or 1

// Fluctuate around a base value (e.g., 100 +/- 15)
float damage = Randomizer.Fluctuate(value: 100f, maxDeviation: 15f);
```

### 4. Collections and Enums

Randomly pick from or shuffle existing data structures with zero unnecessary heap allocations:

```csharp
// Pick a random enum
DamageType type = Randomizer.NextEnum<DamageType>();

// Choose an element from Span or List
ReadOnlySpan<string> biomes = ["Forest", "Desert", "Tundra", "Swamp"];
string chosen = Randomizer.Choose(biomes);

// In-place Fisher-Yates shuffle
Span<int> deck = stackalloc int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
deck.Shuffle();
```

### 5. Deterministic Generation Using Explicit Seeds

For reproducible runs (e.g., procedural world seeds, replay systems), pass an explicit `Seed` instance:

```csharp
// Create a seed with a fixed starting value
Seed mapSeed = new Seed(1337);

// State advances explicitly via 'ref Seed'
Vector3 treePosition = Randomizer.NextUnitVector3(ref mapSeed);
int dungeonRooms = Randomizer.UniformRange(ref mapSeed, min: 5, max: 20);

// Advance manually
++mapSeed;

// Format or parse seeds
Span<char> buffer = stackalloc char[32];
mapSeed.TryFormat(buffer, out int written, default, null);
```

---

## API Summary

| Category | Methods | Notes |
| --- | --- | --- |
| **Primitives** | `NextFloat`, `NextInteger`, `NextLong`, `NextShort`, `NextByte` | Uniform pseudo-random numbers |
| **Ranges** | `UniformRange`, `MinBiasedRange`, `MaxBiasedRange`, `MedianBiasedRange`, `ExtremesBiasedRange` | Generic floating-point & integer ranges |
| **Distribution** | `NextGaussian`, `Fluctuate` | Normal distribution & deviation bounds |
| **Geometry** | `NextUnitVector2`, `NextUnitVector3`, `NextUnitVector4`, `NextUnitCircleVector2`, `NextRotation` | Normalized vectors & Euler-based Quaternions |
| **Collections** | `Choose`, `Shuffle`, `NextEnum<T>` | Supports `Span<T>`, `ReadOnlySpan<T>`, and `IList<T>` |
| **Probability** | `Condition(Chance)` | Fast condition evaluation against predefined `Chance` weights |

### `Chance` Probabilities Reference

* `Chance.Impossible`: $0\%$
* `Chance.Remote`: $1\%$
* `Chance.Unlikely`: $25\%$
* `Chance.Even`: $50\%$
* `Chance.Likely`: $75\%$
* `Chance.Expected`: $99\%$
* `Chance.Certain`: $100\%$

## License

This project is licensed under the **PolyForm Perimeter License 1.0.0**.

> **Summary:** You are free to use, modify, and distribute this software, provided you do not use it to build a product that competes with the software itself.

Please see the [LICENSE.md](https://github.com/gamesharp/Core/blob/master/LICENSE.md) file for the full legal text.