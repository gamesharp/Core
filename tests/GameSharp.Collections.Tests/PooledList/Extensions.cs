using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace GameSharp.Collections.Tests.PooledList;

internal static class Extensions
{
    extension (Assert)
    {
        [StackTraceHidden]
        public static void SequenceEqual<T>(scoped ref readonly PooledList<T> list, List<T> other, IEqualityComparer<T?> comparer)
        {
            Assert.True(list.AsSpan().SequenceEqual(CollectionsMarshal.AsSpan(other), comparer), "The spans are not equal.");
        }
    }
}
