namespace SharpEngine.World.Chunks;

public sealed class Chunk
{
    public const int Size = 16;
    public const int Height = 16;

    private readonly ushort[] _blocks = new ushort[Size * Height * Size];

    public Chunk(ChunkPosition position)
    {
        Position = position;
    }

    public ChunkPosition Position { get; }

    public ushort GetBlock(LocalBlockPosition position)
    {
        return _blocks[GetIndex(position)];
    }

    public void SetBlock(LocalBlockPosition position, ushort blockId)
    {
        _blocks[GetIndex(position)] = blockId;
    }

    private static int GetIndex(LocalBlockPosition position)
    {
        Validate(position);
        return position.X + (position.Z * Size) + (position.Y * Size * Size);
    }

    private static void Validate(LocalBlockPosition position)
    {
        if (position.X is < 0 or >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position.X, "Local X is outside chunk bounds.");
        }

        if (position.Y is < 0 or >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position.Y, "Local Y is outside chunk bounds.");
        }

        if (position.Z is < 0 or >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position.Z, "Local Z is outside chunk bounds.");
        }
    }
}

