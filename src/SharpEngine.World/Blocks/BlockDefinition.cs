namespace SharpEngine.World.Blocks;

public sealed record BlockDefinition(
    ushort Id,
    string Name,
    bool IsSolid,
    bool IsOpaque,
    byte LightEmission,
    ushort TextureIndex = 0)
{
    public bool IsRenderable => IsSolid || IsOpaque || LightEmission > 0;
}

