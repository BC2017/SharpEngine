using OpenTK.Mathematics;

namespace SharpEngine.Rendering;

public readonly record struct DebugOverlaySnapshot(
    bool IsVisible,
    double FramesPerSecond,
    double FrameTimeMilliseconds,
    int FixedTicks,
    Vector3 CameraPosition,
    string InteractionText = "");

