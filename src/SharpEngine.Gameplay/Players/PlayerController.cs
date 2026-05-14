using System.Numerics;

namespace SharpEngine.Gameplay.Players;

public sealed class PlayerController
{
    private const float Gravity = -28.0f;
    private const float JumpVelocity = 8.0f;
    private const float WalkSpeed = 4.6f;
    private const float SprintSpeed = 7.2f;
    private const float CrouchSpeed = 2.2f;
    private const float FlySpeed = 12.0f;
    private const float FlySprintSpeed = 24.0f;
    private const float GroundFriction = 16.0f;
    private const float AirControl = 5.0f;
    private const float StepHeight = 0.55f;

    private static readonly Vector3 PlayerHalfExtents = new(0.30f, 0.90f, 0.30f);
    private static readonly Vector3 CrouchedHalfExtents = new(0.30f, 0.75f, 0.30f);

    public PlayerController(Vector3 spawnPosition)
    {
        Position = spawnPosition;
    }

    public Vector3 Position { get; private set; }

    public Vector3 Velocity { get; private set; }

    public bool IsGrounded { get; private set; }

    public bool IsCrouching { get; private set; }

    public bool IsSwimming { get; private set; }

    public bool IsFlying { get; private set; }

    public Vector3 HalfExtents => IsCrouching ? CrouchedHalfExtents : PlayerHalfExtents;

    public float EyeHeightFromFeet => IsCrouching ? 1.32f : 1.62f;

    public Vector3 EyePosition => Position + new Vector3(0.0f, EyeHeightFromFeet - HalfExtents.Y, 0.0f);

    public void Update(PlayerInput input, float deltaSeconds, Func<Vector3, Vector3, bool> overlapsSolid)
    {
        ArgumentNullException.ThrowIfNull(overlapsSolid);

        if (deltaSeconds <= 0.0f)
        {
            return;
        }

        IsSwimming = false;

        if (IsFlying)
        {
            UpdateFlying(input, deltaSeconds);
            return;
        }

        IsCrouching = input.Crouch;
        Vector3 halfExtents = IsCrouching ? CrouchedHalfExtents : PlayerHalfExtents;
        Vector3 desiredHorizontalVelocity = GetDesiredHorizontalVelocity(input);

        if (IsGrounded)
        {
            Velocity = new Vector3(
                MoveToward(Velocity.X, desiredHorizontalVelocity.X, GroundFriction * deltaSeconds),
                Velocity.Y,
                MoveToward(Velocity.Z, desiredHorizontalVelocity.Z, GroundFriction * deltaSeconds));
        }
        else
        {
            Velocity = new Vector3(
                MoveToward(Velocity.X, desiredHorizontalVelocity.X, AirControl * deltaSeconds),
                Velocity.Y,
                MoveToward(Velocity.Z, desiredHorizontalVelocity.Z, AirControl * deltaSeconds));
        }

        if (input.Jump && IsGrounded && !IsCrouching)
        {
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
            IsGrounded = false;
        }

        Velocity = new Vector3(Velocity.X, Velocity.Y + (Gravity * deltaSeconds), Velocity.Z);

        MoveWithCollision(new Vector3(Velocity.X * deltaSeconds, 0.0f, 0.0f), halfExtents, overlapsSolid, allowStep: true);
        MoveWithCollision(new Vector3(0.0f, 0.0f, Velocity.Z * deltaSeconds), halfExtents, overlapsSolid, allowStep: true);

        IsGrounded = false;
        MoveWithCollision(new Vector3(0.0f, Velocity.Y * deltaSeconds, 0.0f), halfExtents, overlapsSolid, allowStep: false);

        if (Velocity.Y <= 0.0f && IsStandingOnGround(halfExtents, overlapsSolid))
        {
            IsGrounded = true;
            Velocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
        }
    }

    public void Teleport(Vector3 position)
    {
        Position = position;
        Velocity = Vector3.Zero;
        IsGrounded = false;
    }

    public void ToggleFlying()
    {
        IsFlying = !IsFlying;
        IsGrounded = false;
        IsCrouching = false;
        Velocity = Vector3.Zero;
    }

    private void UpdateFlying(PlayerInput input, float deltaSeconds)
    {
        IsGrounded = false;
        IsCrouching = false;

        Vector3 forward = NormalizeOrZero(input.Forward);
        Vector3 right = NormalizeOrZero(input.Right);
        Vector3 movement = Vector3.Zero;

        if (input.MoveForward)
        {
            movement += forward;
        }

        if (input.MoveBackward)
        {
            movement -= forward;
        }

        if (input.MoveRight)
        {
            movement += right;
        }

        if (input.MoveLeft)
        {
            movement -= right;
        }

        if (input.Jump)
        {
            movement += Vector3.UnitY;
        }

        if (input.Crouch)
        {
            movement -= Vector3.UnitY;
        }

        if (movement.LengthSquared() > 0.0f)
        {
            movement = Vector3.Normalize(movement);
        }

        float speed = input.Sprint ? FlySprintSpeed : FlySpeed;
        Velocity = movement * speed;
        Position += Velocity * deltaSeconds;
    }

    private Vector3 GetDesiredHorizontalVelocity(PlayerInput input)
    {
        Vector3 forward = FlattenAndNormalize(input.Forward);
        Vector3 right = FlattenAndNormalize(input.Right);
        Vector3 movement = Vector3.Zero;

        if (input.MoveForward)
        {
            movement += forward;
        }

        if (input.MoveBackward)
        {
            movement -= forward;
        }

        if (input.MoveRight)
        {
            movement += right;
        }

        if (input.MoveLeft)
        {
            movement -= right;
        }

        if (movement.LengthSquared() > 0.0f)
        {
            movement = Vector3.Normalize(movement);
        }

        float speed = input.Crouch ? CrouchSpeed : input.Sprint ? SprintSpeed : WalkSpeed;
        return movement * speed;
    }

    private void MoveWithCollision(
        Vector3 delta,
        Vector3 halfExtents,
        Func<Vector3, Vector3, bool> overlapsSolid,
        bool allowStep)
    {
        if (delta.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        Vector3 target = Position + delta;
        if (!overlapsSolid(target, halfExtents))
        {
            Position = target;
            return;
        }

        if (allowStep && IsGrounded && TryStep(delta, halfExtents, overlapsSolid))
        {
            return;
        }

        if (delta.X != 0.0f)
        {
            Velocity = new Vector3(0.0f, Velocity.Y, Velocity.Z);
        }

        if (delta.Y != 0.0f)
        {
            if (delta.Y < 0.0f)
            {
                IsGrounded = true;
            }

            Velocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
        }

        if (delta.Z != 0.0f)
        {
            Velocity = new Vector3(Velocity.X, Velocity.Y, 0.0f);
        }
    }

    private bool TryStep(Vector3 delta, Vector3 halfExtents, Func<Vector3, Vector3, bool> overlapsSolid)
    {
        Vector3 raisedPosition = Position + new Vector3(0.0f, StepHeight, 0.0f);
        if (overlapsSolid(raisedPosition, halfExtents))
        {
            return false;
        }

        Vector3 steppedTarget = raisedPosition + delta;
        if (overlapsSolid(steppedTarget, halfExtents))
        {
            return false;
        }

        Position = steppedTarget;
        IsGrounded = false;
        return true;
    }

    private bool IsStandingOnGround(Vector3 halfExtents, Func<Vector3, Vector3, bool> overlapsSolid)
    {
        return overlapsSolid(Position + new Vector3(0.0f, -0.04f, 0.0f), halfExtents);
    }

    private static Vector3 FlattenAndNormalize(Vector3 vector)
    {
        vector.Y = 0.0f;
        return NormalizeOrZero(vector);
    }

    private static Vector3 NormalizeOrZero(Vector3 vector)
    {
        return vector.LengthSquared() <= float.Epsilon ? Vector3.Zero : Vector3.Normalize(vector);
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + (Math.Sign(target - current) * maxDelta);
    }
}
