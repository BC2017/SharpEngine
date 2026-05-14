namespace SharpEngine.World.Chunks;

public sealed class Chunk
{
    public const int Size = 16;
    public const int Height = 16;
    public const byte MaxLightLevel = 15;

    private readonly ushort[] _blocks = new ushort[Size * Height * Size];
    private readonly byte[] _sunlight = new byte[Size * Height * Size];

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

    public byte GetSunlight(LocalBlockPosition position)
    {
        return _sunlight[GetIndex(position)];
    }

    public void SetSunlight(LocalBlockPosition position, byte lightLevel)
    {
        _sunlight[GetIndex(position)] = Math.Min(lightLevel, MaxLightLevel);
    }

    public void ClearSunlight()
    {
        Array.Clear(_sunlight);
    }

    public ushort[] CopyBlocks()
    {
        return [.. _blocks];
    }

    public void ReplaceBlocks(IReadOnlyList<ushort> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        if (blocks.Count != _blocks.Length)
        {
            throw new ArgumentException($"Expected {_blocks.Length} block ids, got {blocks.Count}.", nameof(blocks));
        }

        for (int i = 0; i < _blocks.Length; i++)
        {
            _blocks[i] = blocks[i];
        }
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
