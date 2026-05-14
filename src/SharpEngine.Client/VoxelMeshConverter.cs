using SharpEngine.Rendering;
using SharpEngine.World.Meshing;

namespace SharpEngine.Client;

internal static class VoxelMeshConverter
{
    public static VoxelRenderMesh ToRenderMesh(ChunkMeshData mesh)
    {
        return ToRenderMesh(mesh, visibleChunkCount: 1);
    }

    public static VoxelRenderMesh ToRenderMesh(ChunkMeshData mesh, int visibleChunkCount)
    {
        VoxelRenderVertex[] vertices = new VoxelRenderVertex[mesh.Vertices.Count];

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            ChunkMeshVertex vertex = mesh.Vertices[i];
            vertices[i] = new VoxelRenderVertex(
                vertex.X,
                vertex.Y,
                vertex.Z,
                vertex.NormalX,
                vertex.NormalY,
                vertex.NormalZ,
                vertex.U,
                vertex.V,
                vertex.TextureIndex);
        }

        return new VoxelRenderMesh(vertices, [.. mesh.Indices], visibleChunkCount);
    }
}
