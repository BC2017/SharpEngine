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
        float height = SmoothedHeight(worldX, worldZ);
        return Math.Clamp((int)MathF.Round(height), 2, Chunk.Height - 6);
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

    private float SmoothedHeight(int worldX, int worldZ)
    {
        float total = RawHeight(worldX, worldZ) * 4.0f;
        total += RawHeight(worldX - 1, worldZ);
        total += RawHeight(worldX + 1, worldZ);
        total += RawHeight(worldX, worldZ - 1);
        total += RawHeight(worldX, worldZ + 1);
        total += RawHeight(worldX - 1, worldZ - 1) * 0.5f;
        total += RawHeight(worldX + 1, worldZ - 1) * 0.5f;
        total += RawHeight(worldX - 1, worldZ + 1) * 0.5f;
        total += RawHeight(worldX + 1, worldZ + 1) * 0.5f;
        return total / 10.0f;
    }

    private float RawHeight(int worldX, int worldZ)
    {
        float continentalness = FractalPerlin(worldX * 0.010f, worldZ * 0.010f, octaves: 5, persistence: 0.58f);
        float rollingHills = FractalPerlin((worldX + 193) * 0.032f, (worldZ - 71) * 0.032f, octaves: 4, persistence: 0.52f);
        float mountainMask = Smooth01(FractalPerlin((worldX - 521) * 0.0075f, (worldZ + 311) * 0.0075f, octaves: 3, persistence: 0.60f));
        float mountains = Smooth01(RidgedPerlin((worldX + 877) * 0.024f, (worldZ - 443) * 0.024f, octaves: 4, persistence: 0.55f));

        float elevatedLand = Smooth01((continentalness * 0.75f) + 0.22f);
        float foothills = rollingHills * 3.4f;
        float mountainHeight = mountains * mountainMask * 10.5f;
        return 4.0f + (elevatedLand * 11.0f) + foothills + mountainHeight;
    }

    private float FractalPerlin(float x, float z, int octaves, float persistence)
    {
        float total = 0.0f;
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float max = 0.0f;

        for (int octave = 0; octave < octaves; octave++)
        {
            total += Perlin(x * frequency, z * frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2.0f;
        }

        return total / max;
    }

    private float RidgedPerlin(float x, float z, int octaves, float persistence)
    {
        float total = 0.0f;
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float max = 0.0f;

        for (int octave = 0; octave < octaves; octave++)
        {
            float ridge = 1.0f - MathF.Abs(Perlin(x * frequency, z * frequency));
            total += (ridge * ridge) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2.0f;
        }

        return (total / max) * 2.0f - 1.0f;
    }

    private float Perlin(float x, float z)
    {
        int x0 = (int)MathF.Floor(x);
        int z0 = (int)MathF.Floor(z);
        int x1 = x0 + 1;
        int z1 = z0 + 1;
        float localX = x - x0;
        float localZ = z - z0;
        float sx = Fade(localX);
        float sz = Fade(localZ);

        float n00 = GradientDot(x0, z0, localX, localZ);
        float n10 = GradientDot(x1, z0, localX - 1.0f, localZ);
        float n01 = GradientDot(x0, z1, localX, localZ - 1.0f);
        float n11 = GradientDot(x1, z1, localX - 1.0f, localZ - 1.0f);

        float nx0 = Lerp(n00, n10, sx);
        float nx1 = Lerp(n01, n11, sx);
        return Math.Clamp(Lerp(nx0, nx1, sz) * 1.45f, -1.0f, 1.0f);
    }

    private float GradientDot(int x, int z, float offsetX, float offsetZ)
    {
        return (Hash(x, z, _settings.Seed) & 7) switch
        {
            0 => offsetX + offsetZ,
            1 => -offsetX + offsetZ,
            2 => offsetX - offsetZ,
            3 => -offsetX - offsetZ,
            4 => offsetX,
            5 => -offsetX,
            6 => offsetZ,
            _ => -offsetZ,
        };
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

    private static float Fade(float value)
    {
        return value * value * value * (value * ((value * 6.0f) - 15.0f) + 10.0f);
    }

    private static float Smooth01(float value)
    {
        value = Math.Clamp((value + 1.0f) * 0.5f, 0.0f, 1.0f);
        return value * value * (3.0f - (2.0f * value));
    }

    private static float Lerp(float a, float b, float amount)
    {
        return a + ((b - a) * amount);
    }
}
