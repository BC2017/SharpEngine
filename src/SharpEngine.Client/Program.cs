using SharpEngine.Core;
using SharpEngine.Platform.Application;
using SharpEngine.Platform.Windowing;
using SharpEngine.World.Blocks;

BlockRegistry blocks = new();
blocks.Register(new BlockDefinition(0, "sharpengine:air", IsSolid: false, IsOpaque: false, LightEmission: 0));
blocks.Register(new BlockDefinition(1, "sharpengine:stone", IsSolid: true, IsOpaque: true, LightEmission: 0));

Console.WriteLine($"{EngineInfo.Name} client foundation initialized.");
Console.WriteLine($"Registered blocks: {blocks.Blocks.Count}");

if (args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
{
    return;
}

GameHostSettings settings = GameHostSettings.Default with
{
    Title = $"{EngineInfo.Name} - Milestone 1",
    AutoCloseAfter = GetAutoCloseAfter(args)
};

using OpenTkGameHost host = new(settings, new SharpEngine.Client.GameClient());
host.Run();

static TimeSpan? GetAutoCloseAfter(string[] arguments)
{
    const string option = "--auto-close-seconds";

    for (int i = 0; i < arguments.Length - 1; i++)
    {
        if (!string.Equals(arguments[i], option, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return double.TryParse(arguments[i + 1], out double seconds) && seconds > 0.0
            ? TimeSpan.FromSeconds(seconds)
            : throw new ArgumentException($"{option} requires a positive numeric value.");
    }

    return null;
}
