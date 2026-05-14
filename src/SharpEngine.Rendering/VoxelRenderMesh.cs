namespace SharpEngine.Rendering;

public sealed class VoxelRenderMesh
{
    public VoxelRenderMesh(IReadOnlyList<VoxelRenderVertex> vertices, IReadOnlyList<uint> indices, int visibleChunkCount = 1)
    {
        Vertices = vertices;
        Indices = indices;
        VisibleChunkCount = visibleChunkCount;
    }

    public IReadOnlyList<VoxelRenderVertex> Vertices { get; }

    public IReadOnlyList<uint> Indices { get; }

    public int VisibleChunkCount { get; }
}
