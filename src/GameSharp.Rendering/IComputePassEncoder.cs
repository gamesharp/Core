namespace GameSharp.Rendering;

public interface IComputePassEncoder : IDisposable
{
    string? Label { get; set; }

    void DispatchWorkgroups(int workgroupCountX, int workgroupCountY, int workgroupCountZ);

    void DispatchWorkgroupsIndirect<T>(IBuffer<T> indirectBuffer, int startIndex) where T : unmanaged;

    void SetPipeline(IComputePipeline pipeline);

    void SetBindGroup(int groupIndex, IBindGroup bindGroup, ReadOnlySpan<uint> dynamicOffsets = default);

    void InsertDebugMarker(string markerLabel);

    void PushDebugGroup(string groupLabel);

    void PopDebugGroup();
}