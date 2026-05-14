using OpenTK.Mathematics;
using SharpEngine.Core.Time;
using SharpEngine.Platform.Application;
using SharpEngine.Platform.Input;
using SharpEngine.Rendering;
using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;
using SharpEngine.World.Meshing;

namespace SharpEngine.Client;

public sealed class GameClient : IGameApplication
{
    private readonly BlockRegistry _blocks;
    private readonly DebugFrameTimer _frameTimer = new();
    private readonly ChunkMesher _mesher = new();
    private DebugCamera _camera = new(new Vector3(8.0f, 7.0f, 28.0f));
    private OpenGlDebugRenderer? _renderer;
    private int _fixedTicks;

    public GameClient(BlockRegistry blocks)
    {
        _blocks = blocks;
    }

    public void Load(PlatformContext context)
    {
        _renderer = new OpenGlDebugRenderer(context.Width, context.Height);
        Chunk chunk = CreateDemoChunk();
        ChunkMeshData mesh = _mesher.BuildMesh(chunk, _blocks);
        _renderer.LoadChunkMesh(VoxelMeshConverter.ToRenderMesh(mesh));
    }

    public void FixedUpdate(GameTime time)
    {
        _fixedTicks++;
    }

    public void Update(GameTime time, InputSnapshot input)
    {
        _camera.Update(input, (float)time.Delta.TotalSeconds);
    }

    public void Render(GameTime time)
    {
        _frameTimer.RecordFrame(time.Delta);

        _renderer?.RenderFrame(
            _camera,
            time.Total,
            new DebugOverlaySnapshot(
                IsVisible: true,
                _frameTimer.FramesPerSecond,
                _frameTimer.FrameTimeMilliseconds,
                _fixedTicks,
                _camera.Position));
    }

    public void Resize(int width, int height)
    {
        _renderer?.Resize(width, height);
    }

    public void Unload()
    {
        _renderer?.Dispose();
    }

    public int FixedTicks => _fixedTicks;

    private static Chunk CreateDemoChunk()
    {
        Chunk chunk = new(new ChunkPosition(0, 0));

        for (int z = 0; z < Chunk.Size; z++)
        {
            for (int x = 0; x < Chunk.Size; x++)
            {
                int height = 3 + (int)MathF.Round(MathF.Sin(x * 0.55f) + MathF.Cos(z * 0.45f));
                height = Math.Clamp(height, 1, Chunk.Height - 1);

                for (int y = 0; y <= height; y++)
                {
                    ushort blockId = y == height ? BlockIds.Grass : y > height - 3 ? BlockIds.Dirt : BlockIds.Stone;
                    chunk.SetBlock(new LocalBlockPosition(x, y, z), blockId);
                }

                if ((x + z) % 11 == 0 && height + 1 < Chunk.Height)
                {
                    chunk.SetBlock(new LocalBlockPosition(x, height + 1, z), BlockIds.Sand);
                }
            }
        }

        AddTree(chunk, trunkX: 5, groundZ: 5);
        AddTree(chunk, trunkX: 11, groundZ: 10);

        return chunk;
    }

    private static void AddTree(Chunk chunk, int trunkX, int groundZ)
    {
        int groundY = FindHighestSolidBlock(chunk, trunkX, groundZ);

        for (int y = groundY + 1; y <= groundY + 4 && y < Chunk.Height; y++)
        {
            chunk.SetBlock(new LocalBlockPosition(trunkX, y, groundZ), BlockIds.Log);
        }

        for (int y = groundY + 3; y <= groundY + 5 && y < Chunk.Height; y++)
        {
            for (int z = groundZ - 2; z <= groundZ + 2; z++)
            {
                for (int x = trunkX - 2; x <= trunkX + 2; x++)
                {
                    if (x is < 0 or >= Chunk.Size || z is < 0 or >= Chunk.Size)
                    {
                        continue;
                    }

                    int distance = Math.Abs(x - trunkX) + Math.Abs(z - groundZ);
                    if (distance <= 3)
                    {
                        chunk.SetBlock(new LocalBlockPosition(x, y, z), BlockIds.Leaves);
                    }
                }
            }
        }
    }

    private static int FindHighestSolidBlock(Chunk chunk, int x, int z)
    {
        for (int y = Chunk.Height - 1; y >= 0; y--)
        {
            if (chunk.GetBlock(new LocalBlockPosition(x, y, z)) != BlockIds.Air)
            {
                return y;
            }
        }

        return 0;
    }
}

