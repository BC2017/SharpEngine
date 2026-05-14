using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpEngine.Core.Time;
using SharpEngine.Platform.Application;
using SharpEngine.Platform.Input;

namespace SharpEngine.Platform.Windowing;

public sealed class OpenTkGameHost : GameWindow
{
    private readonly IGameApplication _application;
    private readonly TimeSpan? _autoCloseAfter;
    private readonly TimeSpan _fixedStep;
    private TimeSpan _accumulator;
    private TimeSpan _total;

    public OpenTkGameHost(GameHostSettings settings, IGameApplication application)
        : base(
            CreateGameWindowSettings(),
            CreateNativeWindowSettings(settings))
    {
        _application = application;
        _autoCloseAfter = settings.AutoCloseAfter;
        _fixedStep = TimeSpan.FromSeconds(1.0 / settings.FixedTicksPerSecond);
        VSync = settings.VSync ? VSyncMode.On : VSyncMode.Off;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        CursorState = CursorState.Grabbed;
        _application.Load(new PlatformContext(Size.X, Size.Y));
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        TimeSpan delta = TimeSpan.FromSeconds(args.Time);
        _total += delta;
        _accumulator += delta;

        InputSnapshot input = CaptureInput();
        if (input.WantsExit || (_autoCloseAfter is { } autoCloseAfter && _total >= autoCloseAfter))
        {
            Close();
            return;
        }

        while (_accumulator >= _fixedStep)
        {
            _application.FixedUpdate(new GameTime(_total, _fixedStep));
            _accumulator -= _fixedStep;
        }

        _application.Update(new GameTime(_total, delta), input);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _application.Render(new GameTime(_total, TimeSpan.FromSeconds(args.Time)));
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _application.Resize(e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _application.Unload();
        base.OnUnload();
    }

    private InputSnapshot CaptureInput()
    {
        return new InputSnapshot(
            KeyboardState.IsKeyDown(Keys.Escape),
            KeyboardState.IsKeyDown(Keys.W),
            KeyboardState.IsKeyDown(Keys.S),
            KeyboardState.IsKeyDown(Keys.A),
            KeyboardState.IsKeyDown(Keys.D),
            KeyboardState.IsKeyDown(Keys.Space),
            KeyboardState.IsKeyDown(Keys.LeftControl),
            KeyboardState.IsKeyDown(Keys.LeftShift),
            MouseState.Delta.X,
            MouseState.Delta.Y);
    }

    private static GameWindowSettings CreateGameWindowSettings()
    {
        return new GameWindowSettings
        {
            UpdateFrequency = 0
        };
    }

    private static NativeWindowSettings CreateNativeWindowSettings(GameHostSettings settings)
    {
        return new NativeWindowSettings
        {
            Title = settings.Title,
            ClientSize = new Vector2i(settings.Width, settings.Height),
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        };
    }
}
