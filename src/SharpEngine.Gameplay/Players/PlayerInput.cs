using System.Numerics;

namespace SharpEngine.Gameplay.Players;

public readonly record struct PlayerInput(
    bool MoveForward,
    bool MoveBackward,
    bool MoveLeft,
    bool MoveRight,
    bool Jump,
    bool Crouch,
    bool Sprint,
    Vector3 Forward,
    Vector3 Right);

