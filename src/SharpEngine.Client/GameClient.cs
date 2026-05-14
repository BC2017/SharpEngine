using OpenTK.Mathematics;
using SharpEngine.Core.Time;
using SharpEngine.Platform.Application;
using SharpEngine.Platform.Input;
using SharpEngine.Rendering;

namespace SharpEngine.Client;

public sealed class GameClient : IGameApplication
{
    private DebugCamera _camera = new(new Vector3(0.0f, 1.5f, 4.0f));
    private OpenGlDebugRenderer? _renderer;
    private int _fixedTicks;

    public void Load(PlatformContext context)
    {
        _renderer = new OpenGlDebugRenderer(context.Width, context.Height);
    }

    public void FixedUpdate(GameTime time)
    {
        _fixedTicks++;
    }

    public void Update(GameTime time, InputSnapshot input)
    {
        _camera.Update(input, (float)time.Delta.TotalSeconds);
    }

    public void Render(GameTime time)
    {
        _renderer?.RenderFrame(_camera, time.Total);
    }

    public void Resize(int width, int height)
    {
        _renderer?.Resize(width, height);
    }

    public void Unload()
    {
        _renderer?.Dispose();
    }

    public int FixedTicks => _fixedTicks;
}

