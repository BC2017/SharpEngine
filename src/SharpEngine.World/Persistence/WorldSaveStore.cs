using System.IO.Compression;
using System.Text.Json;
using SharpEngine.Core;
using SharpEngine.World.Chunks;
using SharpEngine.World.Generation;

namespace SharpEngine.World.Persistence;

public sealed class WorldSaveStore
{
    private const string MetadataFileName = "world.json";
    private const string ChunksDirectoryName = "chunks";
    private const string ChunkFileExtension = ".chk.gz";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };

    private readonly string _chunksPath;

    private WorldSaveStore(string rootPath, WorldMetadata metadata)
    {
        RootPath = rootPath;
        Metadata = metadata;
        _chunksPath = Path.Combine(rootPath, ChunksDirectoryName);
    }

    public string RootPath { get; }

    public WorldMetadata Metadata { get; private set; }

    public string WorldId => Path.GetFileName(RootPath);

    public int SavedChunkCount => Directory.Exists(_chunksPath)
        ? Directory.EnumerateFiles(_chunksPath, $"*{ChunkFileExtension}", SearchOption.TopDirectoryOnly).Count()
        : 0;

    public static WorldSaveStore OpenOrCreate(string rootPath, string worldName, TerrainGeneratorSettings generatorSettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(worldName);
        ArgumentNullException.ThrowIfNull(generatorSettings);

        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(Path.Combine(rootPath, ChunksDirectoryName));

        string metadataPath = Path.Combine(rootPath, MetadataFileName);
        WorldMetadata metadata = File.Exists(metadataPath)
            ? LoadMetadata(metadataPath)
            : CreateMetadata(worldName, generatorSettings);

        WorldSaveStore store = new(rootPath, metadata);
        store.TouchLastPlayed();
        return store;
    }

    public Chunk? TryLoadChunk(ChunkPosition position)
    {
        string chunkPath = GetChunkPath(position);
        if (!File.Exists(chunkPath))
        {
            return null;
        }

        using FileStream file = File.OpenRead(chunkPath);
        using GZipStream gzip = new(file, CompressionMode.Decompress);
        ChunkSaveData data = JsonSerializer.Deserialize<ChunkSaveData>(gzip, JsonOptions)
            ?? throw new InvalidDataException($"Chunk save '{chunkPath}' is empty.");

        ValidateChunkData(data, position, chunkPath);

        Chunk chunk = new(position);
        chunk.ReplaceBlocks(data.Blocks);
        return chunk;
    }

    public void SaveChunk(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        Directory.CreateDirectory(_chunksPath);

        ChunkSaveData data = new(
            EngineInfo.SaveFormatVersion,
            chunk.Position.X,
            chunk.Position.Z,
            Chunk.Size,
            Chunk.Height,
            chunk.CopyBlocks());

        string chunkPath = GetChunkPath(chunk.Position);
        using FileStream file = File.Create(chunkPath);
        using GZipStream gzip = new(file, CompressionLevel.Fastest);
        JsonSerializer.Serialize(gzip, data, JsonOptions);
    }

    public void SaveChunks(IEnumerable<Chunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        foreach (Chunk chunk in chunks)
        {
            SaveChunk(chunk);
        }

        TouchLastPlayed();
    }

    public void TouchLastPlayed()
    {
        Metadata = Metadata with { LastPlayedUtc = DateTimeOffset.UtcNow };
        SaveMetadata();
    }

    private void SaveMetadata()
    {
        string metadataPath = Path.Combine(RootPath, MetadataFileName);
        using FileStream file = File.Create(metadataPath);
        JsonSerializer.Serialize(file, Metadata, JsonOptions);
    }

    private string GetChunkPath(ChunkPosition position)
    {
        return Path.Combine(_chunksPath, $"c.{position.X}.{position.Z}{ChunkFileExtension}");
    }

    private static WorldMetadata LoadMetadata(string metadataPath)
    {
        using FileStream file = File.OpenRead(metadataPath);
        WorldMetadata metadata = JsonSerializer.Deserialize<WorldMetadata>(file, JsonOptions)
            ?? throw new InvalidDataException($"World metadata '{metadataPath}' is empty.");

        if (metadata.SaveFormatVersion != EngineInfo.SaveFormatVersion)
        {
            throw new InvalidDataException($"Unsupported world save format {metadata.SaveFormatVersion}.");
        }

        return metadata;
    }

    private static WorldMetadata CreateMetadata(string worldName, TerrainGeneratorSettings generatorSettings)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new WorldMetadata(
            EngineInfo.SaveFormatVersion,
            worldName,
            generatorSettings.Seed,
            generatorSettings.WaterLevel,
            now,
            now);
    }

    private static void ValidateChunkData(ChunkSaveData data, ChunkPosition expectedPosition, string chunkPath)
    {
        if (data.SaveFormatVersion != EngineInfo.SaveFormatVersion)
        {
            throw new InvalidDataException($"Unsupported chunk save format {data.SaveFormatVersion} in '{chunkPath}'.");
        }

        if (data.ChunkX != expectedPosition.X || data.ChunkZ != expectedPosition.Z)
        {
            throw new InvalidDataException($"Chunk save '{chunkPath}' contains the wrong chunk position.");
        }

        if (data.Size != Chunk.Size || data.Height != Chunk.Height)
        {
            throw new InvalidDataException($"Chunk save '{chunkPath}' has unsupported dimensions {data.Size}x{data.Height}.");
        }

        if (data.Blocks.Length != Chunk.Size * Chunk.Height * Chunk.Size)
        {
            throw new InvalidDataException($"Chunk save '{chunkPath}' has {data.Blocks.Length} blocks.");
        }
    }
}
