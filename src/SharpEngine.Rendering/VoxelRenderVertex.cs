namespace SharpEngine.Rendering;

public readonly record struct VoxelRenderVertex(
    float X,
    float Y,
    float Z,
    float NormalX,
    float NormalY,
    float NormalZ,
    float U,
    float V,
    ushort TextureIndex,
    float Sunlight);
