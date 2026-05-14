using OpenTK.Mathematics;
using SharpEngine.Platform.Input;

namespace SharpEngine.Rendering;

public sealed class DebugCamera
{
    private const float MouseSensitivity = 0.12f;
    private const float BaseSpeed = 4.5f;
    private const float SprintMultiplier = 3.0f;

    private float _pitch;
    private float _yaw = -90.0f;

    public DebugCamera(Vector3 position)
    {
        Position = position;
    }

    public Vector3 Position { get; private set; }

    public Vector3 Forward
    {
        get
        {
            float yaw = MathHelper.DegreesToRadians(_yaw);
            float pitch = MathHelper.DegreesToRadians(_pitch);

            Vector3 forward = new(
                MathF.Cos(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Sin(yaw) * MathF.Cos(pitch));

            return Vector3.Normalize(forward);
        }
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Forward, Vector3.UnitY);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(70.0f),
            aspectRatio,
            0.05f,
            500.0f);
    }

    public void Update(InputSnapshot input, float deltaSeconds)
    {
        Rotate(input.MouseDeltaX, input.MouseDeltaY);

        Vector3 movement = Vector3.Zero;

        if (input.MoveForward)
        {
            movement += Forward;
        }

        if (input.MoveBackward)
        {
            movement -= Forward;
        }

        if (input.MoveRight)
        {
            movement += Right;
        }

        if (input.MoveLeft)
        {
            movement -= Right;
        }

        if (input.MoveUp)
        {
            movement += Vector3.UnitY;
        }

        if (input.MoveDown)
        {
            movement -= Vector3.UnitY;
        }

        if (movement.LengthSquared > 0.0f)
        {
            movement.Normalize();
        }

        float speed = input.Sprint ? BaseSpeed * SprintMultiplier : BaseSpeed;
        Position += movement * speed * deltaSeconds;
    }

    public void Rotate(float mouseDeltaX, float mouseDeltaY)
    {
        _yaw += mouseDeltaX * MouseSensitivity;
        _pitch -= mouseDeltaY * MouseSensitivity;
        _pitch = Math.Clamp(_pitch, -89.0f, 89.0f);
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    private Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
}

