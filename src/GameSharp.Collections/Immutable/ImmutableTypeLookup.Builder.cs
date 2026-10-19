using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

public sealed partial class ImmutableTypeLookup
{
    /// <summary>
    /// A builder for creating an <see cref="ImmutableTypeLookup"/> instance.
    /// </summary>
    public sealed class Builder : TypeLookup
    {
        private new readonly ref struct ArrayBuilder<T>(ref bool lockHeld, Builder collectionBuilder, TypeLookup.ArrayBuilder<T> arrayBuilder) : IArrayBuilder<T>
        {
            private readonly TypeLookup.ArrayBuilder<T> _arrayBuilder = arrayBuilder;
            private readonly ref bool _lockHeld = ref lockHeld;

            public T[] Array => _arrayBuilder.Array;

            public int LogicalCount
            {
                get => _arrayBuilder.LogicalCount;
                set => _arrayBuilder.LogicalCount = value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Mutate()
            {
                if (!_lockHeld)
                {
                    collectionBuilder._syncRoot.Enter();
                    collectionBuilder.ThrowIfAlreadyBuilt();
                    _lockHeld = true;
                }

                _arrayBuilder.Mutate();
            }
        }

        private volatile bool _isBuilt;
        private readonly Lock _syncRoot = new();

        internal Builder() : base()
        {
        }

        internal Builder(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            : base([.. lookups], lookups.Length, [.. items], items.Length)
        {
        }

        /// <summary>
        /// Converts the builder to an immutable <see cref="ImmutableTypeLookup"/> instance.
        /// </summary>
        /// <remarks>
        /// This method can only be called once. After calling this method, the builder becomes immutable and cannot be modified further.
        /// </remarks>
        /// <returns>An immutable <see cref="ImmutableTypeLookup"/> instance containing the same data as the builder.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the builder has already been built.</exception>
        public ImmutableTypeLookup ToImmutable()
        {
            _syncRoot.Enter();
            try
            {
                ThrowIfAlreadyBuilt();
                _isBuilt = true;
                return new ImmutableTypeLookup(lookups, lookupCount, items, itemCount);
            }
            finally
            {
                _syncRoot.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool Add(object item, TypeInfo typeInfo)
        {
            bool lockHeld = false;
            try
            {
                GetBuilders(ref lockHeld, out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder);
                Add(ref lookupBuilder, ref itemBuilder, item, typeInfo);
                return lockHeld;
            }
            finally
            {
                if (lockHeld)
                {
                    _syncRoot.Exit();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool Remove(object item, TypeInfo typeInfo)
        {
            bool lockHeld = false;
            try
            {
                GetBuilders(ref lockHeld, out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder);
                Remove(ref lookupBuilder, ref itemBuilder, item, typeInfo);
                return lockHeld;
            }
            finally
            {
                if (lockHeld)
                {
                    _syncRoot.Exit();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetBuilders(ref bool lockHeld, out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder)
        {
            GetBuilders(out TypeLookup.ArrayBuilder<IntegerLookup> baseLookupBuilder, out TypeLookup.ArrayBuilder<object> baseItemBuilder);
            lookupBuilder = new ArrayBuilder<IntegerLookup>(ref lockHeld, this, baseLookupBuilder);
            itemBuilder = new ArrayBuilder<object>(ref lockHeld, this, baseItemBuilder);
        }

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfAlreadyBuilt()
        {
            if (_isBuilt)
            {
                BuilderIsAlreadyBuilt();
            }
        }

        [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        private static void BuilderIsAlreadyBuilt()
        {
            throw new InvalidOperationException("The builder has already been built and cannot be modified.");
        }
    }
}
