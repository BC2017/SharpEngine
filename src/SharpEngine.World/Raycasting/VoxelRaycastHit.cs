using SharpEngine.World.Chunks;

namespace SharpEngine.World.Raycasting;

public readonly record struct VoxelRaycastHit(
    LocalBlockPosition Block,
    LocalBlockPosition Adjacent,
    int NormalX,
    int NormalY,
    int NormalZ);

