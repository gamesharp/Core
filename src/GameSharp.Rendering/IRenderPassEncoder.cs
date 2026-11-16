using System.Numerics;

namespace GameSharp.Rendering;

public interface IRenderPassEncoder : IDisposable
{
    void BeginOcclusionQuery(int queryIndex);

    void EndOcclusionQuery();

    void Draw(int vertexCount, int instanceCount, int firstVertex, int firstInstance);

    void DrawIndexed(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance);

    void DrawIndexedIndirect<T>(IBuffer<T> indirectBuffer, int indirectOffset) where T : unmanaged;

    void DrawIndirect<T>(IBuffer<T> indirectBuffer, int indirectOffset) where T : unmanaged;

    void SetVertexBuffer<T>(int slot, IBuffer<T> buffer, int offset = 0, int count = -1) where T : unmanaged;

    void SetIndexBuffer(IBuffer<ushort> buffer, int offset = 0, int count = -1);

    void SetIndexBuffer(IBuffer<uint> buffer, int offset = 0, int count = -1);

    void SetPipeline(IRenderPipeline pipeline);

    void SetBindGroup(int groupIndex, IBindGroup bindGroup, ReadOnlySpan<uint> dynamicOffsets = default);

    void SetViewport(float x, float y, float width, float height, float minDepth = 0f, float maxDepth = 1f);

    void SetScissorRect(int x, int y, int width, int height);
}
