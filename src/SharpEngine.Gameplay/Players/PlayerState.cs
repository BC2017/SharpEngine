using System.Numerics;

namespace SharpEngine.Gameplay.Players;

public sealed record PlayerState(
    Guid Id,
    string Name,
    Vector3 Position,
    Vector3 Velocity);

