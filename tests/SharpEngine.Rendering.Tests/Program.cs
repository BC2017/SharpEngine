using OpenTK.Mathematics;
using SharpEngine.Platform.Input;
using SharpEngine.Rendering;

DebugCamera camera = new(Vector3.Zero);

camera.Update(new InputSnapshot(
    WantsExit: false,
    MoveForward: true,
    MoveBackward: false,
    MoveLeft: false,
    MoveRight: false,
    MoveUp: false,
    MoveDown: false,
    Sprint: false,
    BreakBlock: false,
    PlaceBlock: false,
    SelectedHotbarSlot: -1,
    MouseDeltaX: 0.0f,
    MouseDeltaY: 0.0f), deltaSeconds: 1.0f);

AssertTrue(camera.Position.Z < -4.0f, $"Expected forward movement along -Z, got {camera.Position}.");

camera.Update(new InputSnapshot(
    WantsExit: false,
    MoveForward: false,
    MoveBackward: false,
    MoveLeft: false,
    MoveRight: true,
    MoveUp: false,
    MoveDown: false,
    Sprint: true,
    BreakBlock: false,
    PlaceBlock: false,
    SelectedHotbarSlot: -1,
    MouseDeltaX: 0.0f,
    MouseDeltaY: 0.0f), deltaSeconds: 1.0f);

AssertTrue(camera.Position.X > 13.0f, $"Expected sprinting right movement along +X, got {camera.Position}.");

Console.WriteLine("SharpEngine.Rendering.Tests passed.");

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
