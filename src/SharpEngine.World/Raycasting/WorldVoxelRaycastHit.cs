using SharpEngine.World.Chunks;

namespace SharpEngine.World.Raycasting;

public readonly record struct WorldVoxelRaycastHit(
    BlockPosition Block,
    BlockPosition Adjacent,
    int NormalX,
    int NormalY,
    int NormalZ);

