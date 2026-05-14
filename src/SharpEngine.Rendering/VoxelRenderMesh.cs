namespace SharpEngine.Rendering;

public sealed class VoxelRenderMesh
{
    public VoxelRenderMesh(IReadOnlyList<VoxelRenderVertex> vertices, IReadOnlyList<uint> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public IReadOnlyList<VoxelRenderVertex> Vertices { get; }

    public IReadOnlyList<uint> Indices { get; }
}

