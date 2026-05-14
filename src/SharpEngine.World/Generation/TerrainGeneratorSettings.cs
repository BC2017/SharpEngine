namespace SharpEngine.World.Generation;

public sealed record TerrainGeneratorSettings(
    int Seed,
    int WaterLevel,
    TerrainBlockPalette Blocks);

