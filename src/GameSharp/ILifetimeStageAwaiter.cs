using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameSharp;

/// <summary>
/// Defines a contract for an awaiter that can be used to await the completion of a lifetime stage.
/// </summary>
public interface ILifetimeStageAwaiter : INotifyCompletion
{
    /// <summary>
    /// Gets a value indicating whether this stage has been completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the task representing the asynchronous operation of this lifetime stage.
    /// </summary>
    Task? Task { get; }

    /// <summary>
    /// Gets the result of the asynchronous operation represented by this lifetime stage.
    /// </summary>
    void GetResult();
}
