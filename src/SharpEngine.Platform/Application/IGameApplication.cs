using SharpEngine.Core.Time;
using SharpEngine.Platform.Input;

namespace SharpEngine.Platform.Application;

public interface IGameApplication
{
    void Load(PlatformContext context);

    void FixedUpdate(GameTime time);

    void Update(GameTime time, InputSnapshot input);

    void Render(GameTime time);

    void Resize(int width, int height);

    void Unload();
}

