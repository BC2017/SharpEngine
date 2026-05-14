using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;
using SharpEngine.World.Meshing;

BlockRegistry registry = new();
registry.Register(new BlockDefinition(1, "sharpengine:stone", IsSolid: true, IsOpaque: true, LightEmission: 0));

AssertEqual("sharpengine:stone", registry.Get(1).Name);
AssertEqual(1, registry.Get("sharpengine:stone").Id);

Chunk chunk = new(new ChunkPosition(2, -3));
LocalBlockPosition local = new(1, 2, 3);
chunk.SetBlock(local, 1);

AssertEqual(1, chunk.GetBlock(local));
AssertEqual(new ChunkPosition(-1, -1), new BlockPosition(-1, 0, -1).ToChunkPosition());
AssertEqual(new LocalBlockPosition(15, 0, 15), new BlockPosition(-1, 0, -1).ToLocalBlockPosition());

BlockRegistry meshRegistry = new();
meshRegistry.Register(new BlockDefinition(0, "sharpengine:air", IsSolid: false, IsOpaque: false, LightEmission: 0));
meshRegistry.Register(new BlockDefinition(1, "sharpengine:stone", IsSolid: true, IsOpaque: true, LightEmission: 0, TextureIndex: 3));

ChunkMesher mesher = new();
Chunk singleBlockChunk = new(new ChunkPosition(0, 0));
singleBlockChunk.SetBlock(new LocalBlockPosition(1, 1, 1), 1);
ChunkMeshData singleBlockMesh = mesher.BuildMesh(singleBlockChunk, meshRegistry);

AssertEqual(6, singleBlockMesh.FaceCount);
AssertEqual(24, singleBlockMesh.Vertices.Count);
AssertEqual(36, singleBlockMesh.Indices.Count);

Chunk adjacentBlockChunk = new(new ChunkPosition(0, 0));
adjacentBlockChunk.SetBlock(new LocalBlockPosition(1, 1, 1), 1);
adjacentBlockChunk.SetBlock(new LocalBlockPosition(2, 1, 1), 1);
ChunkMeshData adjacentBlockMesh = mesher.BuildMesh(adjacentBlockChunk, meshRegistry);

AssertEqual(10, adjacentBlockMesh.FaceCount);
AssertEqual(40, adjacentBlockMesh.Vertices.Count);
AssertEqual(60, adjacentBlockMesh.Indices.Count);

Console.WriteLine("SharpEngine.World.Tests passed.");

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}
