namespace SharpEngine.Platform.Application;

public sealed record GameHostSettings(
    string Title,
    int Width,
    int Height,
    int FixedTicksPerSecond,
    bool VSync)
{
    public TimeSpan? AutoCloseAfter { get; init; }

    public static GameHostSettings Default { get; } = new(
        "SharpEngine",
        Width: 1280,
        Height: 720,
        FixedTicksPerSecond: 60,
        VSync: true);
}
