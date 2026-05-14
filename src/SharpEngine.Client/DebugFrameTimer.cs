namespace SharpEngine.Client;

internal sealed class DebugFrameTimer
{
    private TimeSpan _sampleElapsed;
    private int _sampleFrames;

    public double FramesPerSecond { get; private set; }

    public double FrameTimeMilliseconds { get; private set; }

    public void RecordFrame(TimeSpan delta)
    {
        FrameTimeMilliseconds = delta.TotalMilliseconds;
        _sampleElapsed += delta;
        _sampleFrames++;

        if (_sampleElapsed < TimeSpan.FromSeconds(0.25))
        {
            return;
        }

        FramesPerSecond = _sampleFrames / _sampleElapsed.TotalSeconds;
        _sampleElapsed = TimeSpan.Zero;
        _sampleFrames = 0;
    }
}

