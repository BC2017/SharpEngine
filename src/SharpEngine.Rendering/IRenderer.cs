namespace SharpEngine.Rendering;

public interface IRenderer : IDisposable
{
    RenderStats Stats { get; }

    void Resize(int width, int height);

    void SetSelection(VoxelSelectionBox? selection);

    void RenderFrame(DebugCamera camera, TimeSpan totalTime, DebugOverlaySnapshot debugOverlay);
}
