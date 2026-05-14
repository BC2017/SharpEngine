using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;

namespace SharpEngine.World.Meshing;

public sealed class ChunkMesher
{
    public ChunkMeshData BuildMesh(Chunk chunk, BlockRegistry blocks, Func<BlockPosition, bool>? isOpaqueAt = null)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(blocks);

        ChunkMeshData mesh = new();

        for (int y = 0; y < Chunk.Height; y++)
        {
            for (int z = 0; z < Chunk.Size; z++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    LocalBlockPosition position = new(x, y, z);
                    ushort blockId = chunk.GetBlock(position);
                    BlockDefinition block = blocks.Get(blockId);

                    if (!block.IsRenderable)
                    {
                        continue;
                    }

                    AddVisibleFaces(mesh, chunk, blocks, position, block.TextureIndex, isOpaqueAt);
                }
            }
        }

        return mesh;
    }

    private static void AddVisibleFaces(
        ChunkMeshData mesh,
        Chunk chunk,
        BlockRegistry blocks,
        LocalBlockPosition position,
        ushort textureIndex,
        Func<BlockPosition, bool>? isOpaqueAt)
    {
        int x = position.X;
        int y = position.Y;
        int z = position.Z;
        int worldX = (chunk.Position.X * Chunk.Size) + x;
        int worldZ = (chunk.Position.Z * Chunk.Size) + z;

        if (IsFaceVisible(chunk, blocks, isOpaqueAt, x, y, z + 1))
        {
            AddSouthFace(mesh, worldX, y, worldZ, textureIndex);
        }

        if (IsFaceVisible(chunk, blocks, isOpaqueAt, x, y, z - 1))
        {
            AddNorthFace(mesh, worldX, y, worldZ, textureIndex);
        }

        if (IsFaceVisible(chunk, blocks, isOpaqueAt, x - 1, y, z))
        {
            AddWestFace(mesh, worldX, y, worldZ, textureIndex);
        }

        if (IsFaceVisible(chunk, blocks, isOpaqueAt, x + 1, y, z))
        {
            AddEastFace(mesh, worldX, y, worldZ, textureIndex);
        }

        if (IsFaceVisible(chunk, blocks, isOpaqueAt, x, y + 1, z))
        {
            AddTopFace(mesh, worldX, y, worldZ, textureIndex);
        }

        if (IsFaceVisible(chunk, blocks, isOpaqueAt, x, y - 1, z))
        {
            AddBottomFace(mesh, worldX, y, worldZ, textureIndex);
        }
    }

    private static bool IsFaceVisible(
        Chunk chunk,
        BlockRegistry blocks,
        Func<BlockPosition, bool>? isOpaqueAt,
        int x,
        int y,
        int z)
    {
        if (y is < 0 or >= Chunk.Height)
        {
            return true;
        }

        if (x is < 0 or >= Chunk.Size || z is < 0 or >= Chunk.Size)
        {
            if (isOpaqueAt is null)
            {
                return true;
            }

            int worldX = (chunk.Position.X * Chunk.Size) + x;
            int worldZ = (chunk.Position.Z * Chunk.Size) + z;
            return !isOpaqueAt(new BlockPosition(worldX, y, worldZ));
        }

        ushort neighborId = chunk.GetBlock(new LocalBlockPosition(x, y, z));
        BlockDefinition neighbor = blocks.Get(neighborId);
        return !neighbor.IsOpaque;
    }

    private static ChunkMeshVertex Vertex(
        float x,
        float y,
        float z,
        float normalX,
        float normalY,
        float normalZ,
        float u,
        float v,
        ushort textureIndex)
    {
        return new ChunkMeshVertex(x, y, z, normalX, normalY, normalZ, u, v, textureIndex);
    }

    private static void AddSouthFace(ChunkMeshData mesh, int x, int y, int z, ushort textureIndex)
    {
        const float nx = 0.0f;
        const float ny = 0.0f;
        const float nz = 1.0f;
        float z1 = z + 1.0f;

        mesh.AddQuad(
            Vertex(x, y, z1, nx, ny, nz, 0.0f, 0.0f, textureIndex),
            Vertex(x + 1.0f, y, z1, nx, ny, nz, 1.0f, 0.0f, textureIndex),
            Vertex(x + 1.0f, y + 1.0f, z1, nx, ny, nz, 1.0f, 1.0f, textureIndex),
            Vertex(x, y + 1.0f, z1, nx, ny, nz, 0.0f, 1.0f, textureIndex));
    }

    private static void AddNorthFace(ChunkMeshData mesh, int x, int y, int z, ushort textureIndex)
    {
        const float nx = 0.0f;
        const float ny = 0.0f;
        const float nz = -1.0f;

        mesh.AddQuad(
            Vertex(x + 1.0f, y, z, nx, ny, nz, 0.0f, 0.0f, textureIndex),
            Vertex(x, y, z, nx, ny, nz, 1.0f, 0.0f, textureIndex),
            Vertex(x, y + 1.0f, z, nx, ny, nz, 1.0f, 1.0f, textureIndex),
            Vertex(x + 1.0f, y + 1.0f, z, nx, ny, nz, 0.0f, 1.0f, textureIndex));
    }

    private static void AddWestFace(ChunkMeshData mesh, int x, int y, int z, ushort textureIndex)
    {
        const float nx = -1.0f;
        const float ny = 0.0f;
        const float nz = 0.0f;

        mesh.AddQuad(
            Vertex(x, y, z, nx, ny, nz, 0.0f, 0.0f, textureIndex),
            Vertex(x, y, z + 1.0f, nx, ny, nz, 1.0f, 0.0f, textureIndex),
            Vertex(x, y + 1.0f, z + 1.0f, nx, ny, nz, 1.0f, 1.0f, textureIndex),
            Vertex(x, y + 1.0f, z, nx, ny, nz, 0.0f, 1.0f, textureIndex));
    }

    private static void AddEastFace(ChunkMeshData mesh, int x, int y, int z, ushort textureIndex)
    {
        const float nx = 1.0f;
        const float ny = 0.0f;
        const float nz = 0.0f;
        float x1 = x + 1.0f;

        mesh.AddQuad(
            Vertex(x1, y, z + 1.0f, nx, ny, nz, 0.0f, 0.0f, textureIndex),
            Vertex(x1, y, z, nx, ny, nz, 1.0f, 0.0f, textureIndex),
            Vertex(x1, y + 1.0f, z, nx, ny, nz, 1.0f, 1.0f, textureIndex),
            Vertex(x1, y + 1.0f, z + 1.0f, nx, ny, nz, 0.0f, 1.0f, textureIndex));
    }

    private static void AddTopFace(ChunkMeshData mesh, int x, int y, int z, ushort textureIndex)
    {
        const float nx = 0.0f;
        const float ny = 1.0f;
        const float nz = 0.0f;
        float y1 = y + 1.0f;

        mesh.AddQuad(
            Vertex(x, y1, z + 1.0f, nx, ny, nz, 0.0f, 0.0f, textureIndex),
            Vertex(x + 1.0f, y1, z + 1.0f, nx, ny, nz, 1.0f, 0.0f, textureIndex),
            Vertex(x + 1.0f, y1, z, nx, ny, nz, 1.0f, 1.0f, textureIndex),
            Vertex(x, y1, z, nx, ny, nz, 0.0f, 1.0f, textureIndex));
    }

    private static void AddBottomFace(ChunkMeshData mesh, int x, int y, int z, ushort textureIndex)
    {
        const float nx = 0.0f;
        const float ny = -1.0f;
        const float nz = 0.0f;

        mesh.AddQuad(
            Vertex(x, y, z, nx, ny, nz, 0.0f, 0.0f, textureIndex),
            Vertex(x + 1.0f, y, z, nx, ny, nz, 1.0f, 0.0f, textureIndex),
            Vertex(x + 1.0f, y, z + 1.0f, nx, ny, nz, 1.0f, 1.0f, textureIndex),
            Vertex(x, y, z + 1.0f, nx, ny, nz, 0.0f, 1.0f, textureIndex));
    }
}
