namespace SharpEngine.Platform.Input;

public readonly record struct InputSnapshot(
    bool WantsExit,
    bool MoveForward,
    bool MoveBackward,
    bool MoveLeft,
    bool MoveRight,
    bool MoveUp,
    bool MoveDown,
    bool Sprint,
    float MouseDeltaX,
    float MouseDeltaY);
