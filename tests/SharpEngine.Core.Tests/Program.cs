using SharpEngine.Core;

AssertEqual("SharpEngine", EngineInfo.Name);
AssertEqual(1, EngineInfo.SaveFormatVersion);

Console.WriteLine("SharpEngine.Core.Tests passed.");

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

