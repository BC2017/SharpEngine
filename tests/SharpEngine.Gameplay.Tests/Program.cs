using System.Numerics;
using SharpEngine.Gameplay.Players;

PlayerInput emptyInput = CreateEmptyInput();
PlayerController fallingPlayer = new(new Vector3(0.0f, 4.0f, 0.0f));
for (int i = 0; i < 180; i++)
{
    fallingPlayer.Update(emptyInput, 1.0f / 60.0f, GroundOnly);
}

AssertTrue(fallingPlayer.IsGrounded, "Expected player to land on the ground.");
AssertTrue(fallingPlayer.Position.Y is > 1.85f and < 2.10f, $"Unexpected grounded player center Y: {fallingPlayer.Position.Y}.");

fallingPlayer.Update(emptyInput with { Jump = true }, 1.0f / 60.0f, GroundOnly);
AssertTrue(!fallingPlayer.IsGrounded, "Expected jump to leave the ground.");
AssertTrue(fallingPlayer.Velocity.Y > 0.0f, $"Expected upward jump velocity, got {fallingPlayer.Velocity.Y}.");

PlayerController wallPlayer = new(new Vector3(0.0f, 1.9f, 0.0f));
PlayerInput moveRight = emptyInput with
{
    MoveRight = true,
    Right = Vector3.UnitX
};

for (int i = 0; i < 120; i++)
{
    wallPlayer.Update(moveRight, 1.0f / 60.0f, GroundAndWall);
}

AssertTrue(wallPlayer.Position.X < 1.70f, $"Expected wall collision to stop player before x=2, got {wallPlayer.Position.X}.");

PlayerController flyingPlayer = new(new Vector3(0.0f, 2.0f, 0.0f));
flyingPlayer.ToggleFlying();
flyingPlayer.Update(moveRight with { Sprint = true, Jump = true }, 0.5f, GroundAndWall);

AssertTrue(flyingPlayer.IsFlying, "Expected fly mode to stay enabled.");
AssertTrue(flyingPlayer.Position.X > 2.0f, $"Expected fly mode to bypass wall collision, got {flyingPlayer.Position.X}.");
AssertTrue(flyingPlayer.Position.Y > 2.0f, $"Expected fly mode jump input to move upward, got {flyingPlayer.Position.Y}.");

Console.WriteLine("SharpEngine.Gameplay.Tests passed.");

static bool GroundOnly(Vector3 center, Vector3 halfExtents)
{
    return center.Y - halfExtents.Y < 1.0f;
}

static bool GroundAndWall(Vector3 center, Vector3 halfExtents)
{
    bool overlapsGround = center.Y - halfExtents.Y < 1.0f;
    bool overlapsWall = center.X + halfExtents.X > 2.0f &&
        center.X - halfExtents.X < 3.0f &&
        center.Y - halfExtents.Y < 3.0f;

    return overlapsGround || overlapsWall;
}

static PlayerInput CreateEmptyInput()
{
    return new PlayerInput(
        MoveForward: false,
        MoveBackward: false,
        MoveLeft: false,
        MoveRight: false,
        Jump: false,
        Crouch: false,
        Sprint: false,
        Forward: -Vector3.UnitZ,
        Right: Vector3.UnitX);
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
