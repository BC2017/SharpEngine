using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;
using SharpEngine.World.Generation;
using SharpEngine.World.Meshing;
using SharpEngine.World.Raycasting;

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

LocalBlockPosition solidPosition = new(4, 3, 2);
VoxelRaycastHit? hitFromOutside = VoxelRaycaster.Raycast(
    new System.Numerics.Vector3(4.5f, 3.5f, 8.0f),
    new System.Numerics.Vector3(0.0f, 0.0f, -1.0f),
    maxDistance: 12.0f,
    position => position == solidPosition);

AssertEqual(solidPosition, hitFromOutside?.Block);
AssertEqual(new LocalBlockPosition(4, 3, 3), hitFromOutside?.Adjacent);
AssertEqual(1, hitFromOutside?.NormalZ);

VoxelRaycastHit? miss = VoxelRaycaster.Raycast(
    new System.Numerics.Vector3(4.5f, 3.5f, 8.0f),
    new System.Numerics.Vector3(0.0f, 1.0f, 0.0f),
    maxDistance: 4.0f,
    position => position == solidPosition);

AssertEqual(null, miss);

TerrainGenerator generator = new(new TerrainGeneratorSettings(
    Seed: 12345,
    WaterLevel: 4,
    new TerrainBlockPalette(
        Air: 0,
        Grass: 1,
        Dirt: 2,
        Stone: 3,
        Sand: 4,
        Log: 5,
        Leaves: 6)));

Chunk generatedA = generator.GenerateChunk(new ChunkPosition(1, -2));
Chunk generatedB = generator.GenerateChunk(new ChunkPosition(1, -2));
AssertEqual(generatedA.GetBlock(new LocalBlockPosition(4, generator.GetHeight(20, -28), 4)), generatedB.GetBlock(new LocalBlockPosition(4, generator.GetHeight(20, -28), 4)));

int minGeneratedHeight = int.MaxValue;
int maxGeneratedHeight = int.MinValue;
for (int z = -32; z < 32; z++)
{
    for (int x = -32; x < 32; x++)
    {
        int height = generator.GetHeight(x, z);
        minGeneratedHeight = Math.Min(minGeneratedHeight, height);
        maxGeneratedHeight = Math.Max(maxGeneratedHeight, height);
    }
}

AssertTrue(maxGeneratedHeight - minGeneratedHeight >= 5, "Expected Perlin terrain to vary by at least five blocks across the sample area.");

WorldVoxelRaycastHit? worldHit = VoxelRaycaster.RaycastWorld(
    new System.Numerics.Vector3(20.5f, Chunk.Height + 1.0f, -27.5f),
    new System.Numerics.Vector3(0.0f, -1.0f, 0.0f),
    maxDistance: Chunk.Height + 2.0f,
    position => position == new BlockPosition(20, generator.GetHeight(20, -28), -28));

AssertEqual(new BlockPosition(20, generator.GetHeight(20, -28), -28), worldHit?.Block);

Console.WriteLine("SharpEngine.World.Tests passed.");

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
