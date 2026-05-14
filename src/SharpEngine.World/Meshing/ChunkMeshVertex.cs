namespace SharpEngine.World.Meshing;

public readonly record struct ChunkMeshVertex(
    float X,
    float Y,
    float Z,
    float NormalX,
    float NormalY,
    float NormalZ,
    float U,
    float V,
    ushort TextureIndex);

