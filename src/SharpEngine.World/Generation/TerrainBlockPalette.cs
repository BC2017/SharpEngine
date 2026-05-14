namespace SharpEngine.World.Generation;

public readonly record struct TerrainBlockPalette(
    ushort Air,
    ushort Grass,
    ushort Dirt,
    ushort Stone,
    ushort Sand,
    ushort Log,
    ushort Leaves);

