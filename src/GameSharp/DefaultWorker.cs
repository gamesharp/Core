using System.Collections.Concurrent;

namespace GameSharp;

public class DefaultWorker : Worker
{
    /// <inheritdoc/>
    protected internal override Thread Thread { get; }

    public DefaultWorker(BlockingCollection<EnqueuedWorkItem> workItems, string? threadName = null) : base(workItems)
    {
        Thread = new Thread(DoWork)
        { 
            IsBackground = true, 
            Name = threadName
        };
    }
}