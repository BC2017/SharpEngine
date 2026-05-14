using SharpEngine.World.Chunks;

namespace SharpEngine.World.Generation;

public sealed class TerrainGenerator
{
    private readonly TerrainGeneratorSettings _settings;

    public TerrainGenerator(TerrainGeneratorSettings settings)
    {
        _settings = settings;
    }

    public int GetHeight(int worldX, int worldZ)
    {
        float broad = FractalNoise(worldX * 0.055f, worldZ * 0.055f, octaves: 4, persistence: 0.52f);
        float detail = FractalNoise((worldX + 91) * 0.15f, (worldZ - 37) * 0.15f, octaves: 2, persistence: 0.45f);
        int height = 6 + (int)MathF.Round((broad * 4.2f) + (detail * 1.2f));
        return Math.Clamp(height, 2, Chunk.Height - 4);
    }

    public Chunk GenerateChunk(ChunkPosition position)
    {
        Chunk chunk = new(position);

        for (int localZ = 0; localZ < Chunk.Size; localZ++)
        {
            for (int localX = 0; localX < Chunk.Size; localX++)
            {
                int worldX = (position.X * Chunk.Size) + localX;
                int worldZ = (position.Z * Chunk.Size) + localZ;
                int height = GetHeight(worldX, worldZ);
                bool beach = height <= _settings.WaterLevel + 1;

                for (int y = 0; y <= height; y++)
                {
                    ushort blockId = GetTerrainBlock(y, height, beach);
                    chunk.SetBlock(new LocalBlockPosition(localX, y, localZ), blockId);
                }

                if (!beach && ShouldPlaceTree(worldX, worldZ, height))
                {
                    AddTree(chunk, localX, height, localZ);
                }
            }
        }

        return chunk;
    }

    private ushort GetTerrainBlock(int y, int height, bool beach)
    {
        if (y == height)
        {
            return beach ? _settings.Blocks.Sand : _settings.Blocks.Grass;
        }

        if (beach && y > height - 3)
        {
            return _settings.Blocks.Sand;
        }

        return y > height - 3 ? _settings.Blocks.Dirt : _settings.Blocks.Stone;
    }

    private bool ShouldPlaceTree(int worldX, int worldZ, int height)
    {
        if (height >= Chunk.Height - 6)
        {
            return false;
        }

        int hash = Hash(worldX / 3, worldZ / 3, _settings.Seed);
        return worldX % 7 == 0 && worldZ % 7 == 0 && (hash & 7) == 0;
    }

    private void AddTree(Chunk chunk, int localX, int groundY, int localZ)
    {
        if (localX is < 2 or > Chunk.Size - 3 || localZ is < 2 or > Chunk.Size - 3)
        {
            return;
        }

        int trunkHeight = 3 + (Hash(localX, localZ, _settings.Seed) & 1);
        for (int y = groundY + 1; y <= groundY + trunkHeight && y < Chunk.Height; y++)
        {
            chunk.SetBlock(new LocalBlockPosition(localX, y, localZ), _settings.Blocks.Log);
        }

        int leafBase = groundY + trunkHeight - 1;
        for (int y = leafBase; y <= leafBase + 2 && y < Chunk.Height; y++)
        {
            for (int z = localZ - 2; z <= localZ + 2; z++)
            {
                for (int x = localX - 2; x <= localX + 2; x++)
                {
                    int distance = Math.Abs(x - localX) + Math.Abs(z - localZ);
                    if (distance <= 3 && x is >= 0 and < Chunk.Size && z is >= 0 and < Chunk.Size)
                    {
                        chunk.SetBlock(new LocalBlockPosition(x, y, z), _settings.Blocks.Leaves);
                    }
                }
            }
        }
    }

    private float FractalNoise(float x, float z, int octaves, float persistence)
    {
        float total = 0.0f;
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float max = 0.0f;

        for (int octave = 0; octave < octaves; octave++)
        {
            total += ValueNoise(x * frequency, z * frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2.0f;
        }

        return total / max;
    }

    private float ValueNoise(float x, float z)
    {
        int x0 = (int)MathF.Floor(x);
        int z0 = (int)MathF.Floor(z);
        int x1 = x0 + 1;
        int z1 = z0 + 1;
        float sx = SmoothStep(x - x0);
        float sz = SmoothStep(z - z0);

        float n00 = HashToUnit(x0, z0);
        float n10 = HashToUnit(x1, z0);
        float n01 = HashToUnit(x0, z1);
        float n11 = HashToUnit(x1, z1);

        float nx0 = Lerp(n00, n10, sx);
        float nx1 = Lerp(n01, n11, sx);
        return Lerp(nx0, nx1, sz);
    }

    private float HashToUnit(int x, int z)
    {
        return (Hash(x, z, _settings.Seed) / (float)int.MaxValue) * 2.0f - 1.0f;
    }

    private static int Hash(int x, int z, int seed)
    {
        unchecked
        {
            int hash = seed;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ z;
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            return hash & int.MaxValue;
        }
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3.0f - (2.0f * value));
    }

    private static float Lerp(float a, float b, float amount)
    {
        return a + ((b - a) * amount);
    }
}

