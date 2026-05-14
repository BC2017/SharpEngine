namespace SharpEngine.World.Persistence;

public sealed record WorldMetadata(
    int SaveFormatVersion,
    string WorldName,
    int Seed,
    int WaterLevel,
    DateTimeOffset CreatedUtc,
    DateTimeOffset LastPlayedUtc);
