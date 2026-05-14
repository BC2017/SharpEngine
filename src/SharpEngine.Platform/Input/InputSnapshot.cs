namespace SharpEngine.Platform.Input;

public readonly record struct InputSnapshot(
    bool WantsExit,
    int MouseDeltaX,
    int MouseDeltaY);

