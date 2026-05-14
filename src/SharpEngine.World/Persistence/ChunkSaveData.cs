namespace SharpEngine.World.Persistence;

internal sealed record ChunkSaveData(
    int SaveFormatVersion,
    int ChunkX,
    int ChunkZ,
    int Size,
    int Height,
    ushort[] Blocks);
