namespace SharpEngine.Rendering;

public interface IRenderer : IDisposable
{
    RenderStats Stats { get; }

    void Resize(int width, int height);

    void RenderFrame(DebugCamera camera, TimeSpan totalTime, DebugOverlaySnapshot debugOverlay);
}
