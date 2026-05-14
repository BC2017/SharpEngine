namespace SharpEngine.World.Meshing;

public sealed class ChunkMeshData
{
    private readonly List<uint> _indices = [];
    private readonly List<ChunkMeshVertex> _vertices = [];

    public IReadOnlyList<ChunkMeshVertex> Vertices => _vertices;

    public IReadOnlyList<uint> Indices => _indices;

    public int FaceCount => _indices.Count / 6;

    public bool IsEmpty => _indices.Count == 0;

    public void Append(ChunkMeshData other)
    {
        ArgumentNullException.ThrowIfNull(other);

        uint vertexOffset = (uint)_vertices.Count;
        _vertices.AddRange(other._vertices);

        foreach (uint index in other._indices)
        {
            _indices.Add(index + vertexOffset);
        }
    }

    public void AddQuad(
        ChunkMeshVertex bottomLeft,
        ChunkMeshVertex bottomRight,
        ChunkMeshVertex topRight,
        ChunkMeshVertex topLeft)
    {
        uint start = (uint)_vertices.Count;

        _vertices.Add(bottomLeft);
        _vertices.Add(bottomRight);
        _vertices.Add(topRight);
        _vertices.Add(topLeft);

        _indices.Add(start);
        _indices.Add(start + 1);
        _indices.Add(start + 2);
        _indices.Add(start);
        _indices.Add(start + 2);
        _indices.Add(start + 3);
    }
}
