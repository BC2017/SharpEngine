using SharpEngine.Platform.Input;

namespace SharpEngine.Platform;

public interface IPlatformHost
{
    InputSnapshot PollInput();
}

