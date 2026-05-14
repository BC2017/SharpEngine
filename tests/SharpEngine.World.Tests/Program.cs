using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;

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

Console.WriteLine("SharpEngine.World.Tests passed.");

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}
