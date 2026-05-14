namespace SharpEngine.World.Chunks;

public readonly record struct BlockPosition(int X, int Y, int Z)
{
    public ChunkPosition ToChunkPosition()
    {
        return new ChunkPosition(FloorDiv(X, Chunk.Size), FloorDiv(Z, Chunk.Size));
    }

    public LocalBlockPosition ToLocalBlockPosition()
    {
        return new LocalBlockPosition(FloorMod(X, Chunk.Size), Y, FloorMod(Z, Chunk.Size));
    }

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static int FloorMod(int value, int divisor)
    {
        int remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }
}

