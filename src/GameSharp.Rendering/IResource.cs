using System.Runtime.InteropServices;
namespace GameSharp.Rendering;

public interface IResource : IDisposable
{
    string? Label { get; set; }

    SafeHandle Handle { get; }
}
