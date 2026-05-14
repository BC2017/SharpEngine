using SharpEngine.Core;
using SharpEngine.World.Blocks;

BlockRegistry blocks = new();
blocks.Register(new BlockDefinition(0, "sharpengine:air", IsSolid: false, IsOpaque: false, LightEmission: 0));
blocks.Register(new BlockDefinition(1, "sharpengine:stone", IsSolid: true, IsOpaque: true, LightEmission: 0));

Console.WriteLine($"{EngineInfo.Name} client foundation initialized.");
Console.WriteLine($"Registered blocks: {blocks.Blocks.Count}");

