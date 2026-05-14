namespace SharpEngine.World.Blocks;

public sealed record BlockDefinition(
    ushort Id,
    string Name,
    bool IsSolid,
    bool IsOpaque,
    byte LightEmission);

