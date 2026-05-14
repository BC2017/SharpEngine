namespace SharpEngine.Rendering;

public interface IRenderer : IDisposable
{
    RenderStats Stats { get; }

    void RenderFrame();
}

