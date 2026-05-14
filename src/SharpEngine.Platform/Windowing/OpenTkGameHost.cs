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
    private bool _wasLeftMouseDown;
    private bool _wasRightMouseDown;
    private bool _wasF5Down;
    private bool _wasF9Down;

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
        bool isLeftMouseDown = MouseState.IsButtonDown(MouseButton.Left);
        bool isRightMouseDown = MouseState.IsButtonDown(MouseButton.Right);
        bool isF5Down = KeyboardState.IsKeyDown(Keys.F5);
        bool isF9Down = KeyboardState.IsKeyDown(Keys.F9);
        bool breakBlock = isLeftMouseDown && !_wasLeftMouseDown;
        bool placeBlock = isRightMouseDown && !_wasRightMouseDown;
        bool saveWorld = isF5Down && !_wasF5Down;
        bool createWorld = isF9Down && !_wasF9Down;

        _wasLeftMouseDown = isLeftMouseDown;
        _wasRightMouseDown = isRightMouseDown;
        _wasF5Down = isF5Down;
        _wasF9Down = isF9Down;

        return new InputSnapshot(
            KeyboardState.IsKeyDown(Keys.Escape),
            KeyboardState.IsKeyDown(Keys.W),
            KeyboardState.IsKeyDown(Keys.S),
            KeyboardState.IsKeyDown(Keys.A),
            KeyboardState.IsKeyDown(Keys.D),
            KeyboardState.IsKeyDown(Keys.Space),
            KeyboardState.IsKeyDown(Keys.LeftControl),
            KeyboardState.IsKeyDown(Keys.LeftShift),
            breakBlock,
            placeBlock,
            saveWorld,
            createWorld,
            GetSelectedHotbarSlot(),
            MouseState.Delta.X,
            MouseState.Delta.Y);
    }

    private int GetSelectedHotbarSlot()
    {
        if (KeyboardState.IsKeyDown(Keys.D1))
        {
            return 0;
        }

        if (KeyboardState.IsKeyDown(Keys.D2))
        {
            return 1;
        }

        if (KeyboardState.IsKeyDown(Keys.D3))
        {
            return 2;
        }

        if (KeyboardState.IsKeyDown(Keys.D4))
        {
            return 3;
        }

        if (KeyboardState.IsKeyDown(Keys.D5))
        {
            return 4;
        }

        if (KeyboardState.IsKeyDown(Keys.D6))
        {
            return 5;
        }

        return -1;
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
