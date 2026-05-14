using OpenTK.Mathematics;
using SharpEngine.Core.Time;
using SharpEngine.Gameplay.Players;
using SharpEngine.Platform.Application;
using SharpEngine.Platform.Input;
using SharpEngine.Rendering;
using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;
using SharpEngine.World.Meshing;
using SharpEngine.World.Raycasting;

using NumericsVector3 = System.Numerics.Vector3;

namespace SharpEngine.Client;

public sealed class GameClient : IGameApplication
{
    private readonly BlockRegistry _blocks;
    private readonly DebugFrameTimer _frameTimer = new();
    private readonly ushort[] _hotbarBlocks =
    [
        BlockIds.Grass,
        BlockIds.Dirt,
        BlockIds.Stone,
        BlockIds.Sand,
        BlockIds.Log,
        BlockIds.Leaves
    ];

    private readonly ChunkMesher _mesher = new();
    private readonly PlayerController _player = new(new NumericsVector3(8.5f, 9.0f, 12.5f));
    private DebugCamera _camera = new(new Vector3(8.5f, 10.5f, 12.5f));
    private Chunk? _chunk;
    private OpenGlDebugRenderer? _renderer;
    private VoxelRaycastHit? _selection;
    private int _editCount;
    private int _fixedTicks;
    private int _selectedHotbarSlot;

    public GameClient(BlockRegistry blocks)
    {
        _blocks = blocks;
    }

    public void Load(PlatformContext context)
    {
        _renderer = new OpenGlDebugRenderer(context.Width, context.Height);
        _chunk = CreateDemoChunk();
        MovePlayerToSpawn();
        SyncCameraToPlayer();
        RebuildChunkMesh();
    }

    public void FixedUpdate(GameTime time)
    {
        _fixedTicks++;
    }

    public void Update(GameTime time, InputSnapshot input)
    {
        _camera.Rotate(input.MouseDeltaX, input.MouseDeltaY);
        _player.Update(
            CreatePlayerInput(input),
            (float)time.Delta.TotalSeconds,
            IsAabbOverlappingSolid);
        SyncCameraToPlayer();

        if (input.SelectedHotbarSlot >= 0 && input.SelectedHotbarSlot < _hotbarBlocks.Length)
        {
            _selectedHotbarSlot = input.SelectedHotbarSlot;
        }

        UpdateSelection();

        if (input.BreakBlock)
        {
            BreakSelectedBlock();
        }

        if (input.PlaceBlock)
        {
            PlaceSelectedBlock();
        }
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
                _camera.Position,
                GetInteractionDebugText()));
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

    private void UpdateSelection()
    {
        if (_chunk is null)
        {
            _selection = null;
            _renderer?.SetSelection(null);
            return;
        }

        NumericsVector3 origin = new(_camera.Position.X, _camera.Position.Y, _camera.Position.Z);
        NumericsVector3 direction = new(_camera.Forward.X, _camera.Forward.Y, _camera.Forward.Z);

        _selection = VoxelRaycaster.Raycast(
            origin,
            direction,
            maxDistance: 8.0f,
            position => _blocks.Get(_chunk.GetBlock(position)).IsSolid);

        _renderer?.SetSelection(_selection is { } hit
            ? new VoxelSelectionBox(hit.Block.X, hit.Block.Y, hit.Block.Z)
            : null);
    }

    private void BreakSelectedBlock()
    {
        if (_chunk is null || _selection is not { } hit)
        {
            return;
        }

        _chunk.SetBlock(hit.Block, BlockIds.Air);
        _editCount++;
        RebuildChunkMesh();
        UpdateSelection();
    }

    private void PlaceSelectedBlock()
    {
        if (_chunk is null || _selection is not { } hit || !IsInsideChunk(hit.Adjacent))
        {
            return;
        }

        if (_chunk.GetBlock(hit.Adjacent) != BlockIds.Air || WouldBlockIntersectPlayer(hit.Adjacent))
        {
            return;
        }

        _chunk.SetBlock(hit.Adjacent, _hotbarBlocks[_selectedHotbarSlot]);
        _editCount++;
        RebuildChunkMesh();
        UpdateSelection();
    }

    private void RebuildChunkMesh()
    {
        if (_chunk is null)
        {
            return;
        }

        ChunkMeshData mesh = _mesher.BuildMesh(_chunk, _blocks);
        _renderer?.LoadChunkMesh(VoxelMeshConverter.ToRenderMesh(mesh));
    }

    private string GetInteractionDebugText()
    {
        string selectedBlockName = _blocks.Get(_hotbarBlocks[_selectedHotbarSlot]).Name.Replace("sharpengine:", string.Empty, StringComparison.Ordinal);
        string selectionText = _selection is { } hit
            ? $"{hit.Block.X},{hit.Block.Y},{hit.Block.Z}"
            : "NONE";

        string motionText = _player.IsSwimming ? "SWIM" : _player.IsCrouching ? "CROUCH" : _player.IsGrounded ? "GROUND" : "AIR";
        return $"SEL: {selectionText}\nHOTBAR: {_selectedHotbarSlot + 1}/{selectedBlockName}  EDITS: {_editCount}\nMOTION: {motionText}\nVEL: {_player.Velocity.X:0.0},{_player.Velocity.Y:0.0},{_player.Velocity.Z:0.0}";
    }

    private static bool IsInsideChunk(LocalBlockPosition position)
    {
        return position.X is >= 0 and < Chunk.Size &&
            position.Y is >= 0 and < Chunk.Height &&
            position.Z is >= 0 and < Chunk.Size;
    }

    private PlayerInput CreatePlayerInput(InputSnapshot input)
    {
        NumericsVector3 forward = new(_camera.Forward.X, _camera.Forward.Y, _camera.Forward.Z);
        Vector3 cameraRight = Vector3.Normalize(Vector3.Cross(_camera.Forward, Vector3.UnitY));
        NumericsVector3 right = new(cameraRight.X, cameraRight.Y, cameraRight.Z);

        return new PlayerInput(
            input.MoveForward,
            input.MoveBackward,
            input.MoveLeft,
            input.MoveRight,
            Jump: input.MoveUp,
            Crouch: input.MoveDown,
            input.Sprint,
            forward,
            right);
    }

    private bool IsAabbOverlappingSolid(NumericsVector3 center, NumericsVector3 halfExtents)
    {
        if (_chunk is null)
        {
            return false;
        }

        int minX = (int)MathF.Floor(center.X - halfExtents.X);
        int maxX = (int)MathF.Floor(center.X + halfExtents.X - 0.001f);
        int minY = (int)MathF.Floor(center.Y - halfExtents.Y);
        int maxY = (int)MathF.Floor(center.Y + halfExtents.Y - 0.001f);
        int minZ = (int)MathF.Floor(center.Z - halfExtents.Z);
        int maxZ = (int)MathF.Floor(center.Z + halfExtents.Z - 0.001f);

        for (int y = minY; y <= maxY; y++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x is < 0 or >= Chunk.Size || z is < 0 or >= Chunk.Size || y < 0)
                    {
                        return true;
                    }

                    if (y >= Chunk.Height)
                    {
                        continue;
                    }

                    ushort blockId = _chunk.GetBlock(new LocalBlockPosition(x, y, z));
                    if (_blocks.Get(blockId).IsSolid)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool WouldBlockIntersectPlayer(LocalBlockPosition block)
    {
        NumericsVector3 min = new(block.X, block.Y, block.Z);
        NumericsVector3 max = min + NumericsVector3.One;
        NumericsVector3 playerMin = _player.Position - _player.HalfExtents;
        NumericsVector3 playerMax = _player.Position + _player.HalfExtents;

        return min.X < playerMax.X && max.X > playerMin.X &&
            min.Y < playerMax.Y && max.Y > playerMin.Y &&
            min.Z < playerMax.Z && max.Z > playerMin.Z;
    }

    private void SyncCameraToPlayer()
    {
        NumericsVector3 eye = _player.EyePosition;
        _camera.SetPosition(new Vector3(eye.X, eye.Y, eye.Z));
    }

    private void MovePlayerToSpawn()
    {
        int groundY = _chunk is null ? 0 : FindHighestSolidBlock(_chunk, 8, 12);
        NumericsVector3 spawn = new(8.5f, groundY + 1.0f + _player.HalfExtents.Y, 12.5f);
        _player.Teleport(spawn);
    }

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

