using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace GameSharp.Collections.Collectible;

internal class TypeRegistryProvider : Collections.TypeRegistryProvider
{
    private sealed class PurgeContext(List<CollectibleTypeRegistry> removed)
    {
        public TypeRegistry? Retained { get; set; }
        public void Purge(int removedIndex) => Retained!.Purge(removed[removedIndex].AssemblyID);
    }

    private volatile ImmutableDictionary<Assembly, TypeRegistry> _registriesByAssembly;
    private volatile TypeRegistry?[] _registriesByID;
    private ImmutableHashSet<AssemblyLoadContext> _loadContexts;
    private readonly Queue<short> _availableAssemblyIDs;
    private readonly Lock _syncRoot;

    public TypeRegistryProvider()
    {
        _registriesByAssembly = [];
        _registriesByID = [];
        _loadContexts = [AssemblyLoadContext.Default];
        _availableAssemblyIDs = [];
        _syncRoot = new();
    }

    public override TypeRegistry GetOrAdd(Type type)
    {
        Assembly assembly = type.Assembly;

        ImmutableDictionary<Assembly, TypeRegistry> currRegistries = _registriesByAssembly;
        if (currRegistries.TryGetValue(assembly, out TypeRegistry? registry))
        {
            return registry;
        }

        lock (_syncRoot)
        {
            (ImmutableDictionary<Assembly, TypeRegistry> prevRegistries, currRegistries) = (currRegistries, _registriesByAssembly);

            if (!ReferenceEquals(prevRegistries, currRegistries) && currRegistries.TryGetValue(assembly, out registry))
            {
                return registry;
            }

            TypeRegistry?[] registriesByID = _registriesByID;
            short id = checked((short)registriesByID.Length);
            registry = assembly.IsCollectible ? new CollectibleTypeRegistry(id) : new NonCollectibleTypeRegistry(id);
            _registriesByAssembly = _registriesByAssembly.Add(assembly, registry);
            if (_availableAssemblyIDs.TryDequeue(out short assemblyID))
            {
                _registriesByID = [.. registriesByID.AsSpan(..assemblyID), registry, .. registriesByID.AsSpan(assemblyID + 1)];
            }
            else
            {
                _registriesByID = [.. registriesByID, registry];
            }
        }

        if (assembly.IsCollectible && AssemblyLoadContext.GetLoadContext(assembly) is { } loadContext && TryAddLoadContext(loadContext))
        {
            loadContext.Unloading += OnUnloading;
        }

        return registry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool TryGet(Type type, [NotNullWhen(true)] out Collections.TypeRegistry? registry)
    {
        Unsafe.SkipInit(out registry);
        return _registriesByAssembly.TryGetValue(type.Assembly, out Unsafe.As<Collections.TypeRegistry?, TypeRegistry?>(ref registry));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override TypeRegistry Get(int id)
    {
        return _registriesByID[((TypeIdentifier)id).AssemblyID]!;
    }

    private void OnUnloading(AssemblyLoadContext context)
    {
        if (!TryRemoveLoadContext(context)
            || !RemoveAssemblies(context.Assemblies, out ReadOnlySpan<TypeRegistry?> remaining, out List<CollectibleTypeRegistry> removed))
        {
            return;
        }

        try
        {
            foreach (CollectibleTypeRegistry registry in removed)
            {
                registry.Close();
            }

            PurgeContext ctx = new(removed);
            foreach (TypeRegistry? registry in remaining)
            {
                if (registry is null)
                {
                    continue;
                }

                ctx.Retained = registry;
                Parallel.For(0, removed.Count, ctx.Purge);
            }
        }
        finally
        {
            foreach (CollectibleTypeRegistry registry in removed)
            {
                registry.Dispose();
            }

            lock (_syncRoot)
            {
                _availableAssemblyIDs.EnsureCapacity(_availableAssemblyIDs.Count + removed.Count);
                foreach (TypeRegistry registry in removed)
                {
                    _availableAssemblyIDs.Enqueue(registry.AssemblyID);
                }
            }
        }
    }

    private bool RemoveAssemblies(IEnumerable<Assembly> assemblies, out ReadOnlySpan<TypeRegistry?> remaining, out List<CollectibleTypeRegistry> removed)
    {
        removed = [];

        lock (_syncRoot)
        {
            ImmutableDictionary<Assembly, TypeRegistry>.Builder byAssemblyBuilder = _registriesByAssembly.ToBuilder();
            TypeRegistry?[] byID = [.. _registriesByID];

            foreach (Assembly assembly in assemblies)
            {
                if (byAssemblyBuilder.Remove(assembly, out TypeRegistry? registry))
                {
                    byID[registry.AssemblyID] = null;
                    removed.Add((CollectibleTypeRegistry)registry);
                }
            }

            if (removed.Count == 0)
            {
                remaining = _registriesByID.AsSpan();
                return false;
            }

            _registriesByAssembly = byAssemblyBuilder.ToImmutable();
            _registriesByID = byID;
            remaining = _registriesByID.AsSpan();
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryAddLoadContext(AssemblyLoadContext loadContext)
    {
        return ImmutableInterlocked.Update(ref _loadContexts, static (set, ctx) => set.Add(ctx), loadContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryRemoveLoadContext(AssemblyLoadContext loadContext)
    {
        return ImmutableInterlocked.Update(ref _loadContexts, static (set, ctx) => set.Remove(ctx), loadContext);
    }
}
