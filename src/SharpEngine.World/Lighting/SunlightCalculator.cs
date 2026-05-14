using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;

namespace SharpEngine.World.Lighting;

public sealed class SunlightCalculator
{
    public void RebuildSunlight(Chunk chunk, BlockRegistry blocks)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(blocks);

        chunk.ClearSunlight();

        for (int z = 0; z < Chunk.Size; z++)
        {
            for (int x = 0; x < Chunk.Size; x++)
            {
                bool skyVisible = true;

                for (int y = Chunk.Height - 1; y >= 0; y--)
                {
                    LocalBlockPosition position = new(x, y, z);
                    ushort blockId = chunk.GetBlock(position);
                    BlockDefinition block = blocks.Get(blockId);

                    if (skyVisible && !block.IsOpaque)
                    {
                        chunk.SetSunlight(position, Chunk.MaxLightLevel);
                        continue;
                    }

                    if (block.IsOpaque)
                    {
                        skyVisible = false;
                    }
                }
            }
        }
    }
}
