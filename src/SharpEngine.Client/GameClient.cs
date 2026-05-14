using OpenTK.Mathematics;
using SharpEngine.Core.Time;
using SharpEngine.Gameplay.Players;
using SharpEngine.Platform.Application;
using SharpEngine.Platform.Input;
using SharpEngine.Rendering;
using SharpEngine.World.Blocks;
using SharpEngine.World.Chunks;
using SharpEngine.World.Generation;
using SharpEngine.World.Lighting;
using SharpEngine.World.Meshing;
using SharpEngine.World.Persistence;
using SharpEngine.World.Raycasting;

using NumericsVector3 = System.Numerics.Vector3;

namespace SharpEngine.Client;

public sealed class GameClient : IGameApplication
{
    private const int DefaultRenderRadius = 3;

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

    private readonly Dictionary<ChunkPosition, Chunk> _chunks = [];
    private readonly ChunkMesher _mesher = new();
    private readonly SunlightCalculator _sunlightCalculator = new();
    private readonly PlayerController _player = new(new NumericsVector3(8.5f, 9.0f, 12.5f));
    private DebugCamera _camera = new(new Vector3(8.5f, 10.5f, 12.5f));
    private TerrainGenerator? _terrainGenerator;
    private WorldSaveStore? _worldSaveStore;
    private OpenGlDebugRenderer? _renderer;
    private WorldVoxelRaycastHit? _selection;
    private int _editCount;
    private int _fixedTicks;
    private int _loadedRadius;
    private int _streamedChunkLoads;
    private int _streamedChunkUnloads;
    private int _savedChunkCount;
    private int _selectedHotbarSlot;
    private ChunkPosition? _streamingCenter;
    private string _worldUiStatus = "WORLD READY";

    public GameClient(BlockRegistry blocks)
    {
        _blocks = blocks;
    }

    public void Load(PlatformContext context)
    {
        _renderer = new OpenGlDebugRenderer(context.Width, context.Height);
        TerrainGeneratorSettings defaultSettings = CreateTerrainGeneratorSettings(seed: 19790503, waterLevel: 4);
        _worldSaveStore = WorldSaveStore.OpenOrCreate(GetDefaultWorldPath(), "Dev World", defaultSettings);
        _terrainGenerator = new TerrainGenerator(CreateTerrainGeneratorSettings(
            _worldSaveStore.Metadata.Seed,
            _worldSaveStore.Metadata.WaterLevel));
        _savedChunkCount = _worldSaveStore.SavedChunkCount;
        LoadInitialChunks(DefaultRenderRadius);
        RebuildWorldLighting();
        MovePlayerToSpawn();
        UpdateLoadedChunksForPlayer();
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

        if (input.ToggleFly)
        {
            _player.ToggleFlying();
            _worldUiStatus = _player.IsFlying ? "FLY ENABLED" : "FLY DISABLED";
        }

        UpdateLoadedChunksForPlayer();
        _player.Update(
            CreatePlayerInput(input),
            (float)time.Delta.TotalSeconds,
            IsAabbOverlappingSolid);
        UpdateLoadedChunksForPlayer();
        SyncCameraToPlayer();

        if (input.SelectedHotbarSlot >= 0 && input.SelectedHotbarSlot < _hotbarBlocks.Length)
        {
            _selectedHotbarSlot = input.SelectedHotbarSlot;
        }

        if (input.SaveWorld)
        {
            SaveLoadedWorld();
        }

        if (input.CreateWorld)
        {
            CreateNewWorld();
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
        _worldSaveStore?.TouchLastPlayed();
        _renderer?.Dispose();
    }

    public int FixedTicks => _fixedTicks;

    private void UpdateSelection()
    {
        if (_chunks.Count == 0)
        {
            _selection = null;
            _renderer?.SetSelection(null);
            return;
        }

        NumericsVector3 origin = new(_camera.Position.X, _camera.Position.Y, _camera.Position.Z);
        NumericsVector3 direction = new(_camera.Forward.X, _camera.Forward.Y, _camera.Forward.Z);

        _selection = VoxelRaycaster.RaycastWorld(
            origin,
            direction,
            maxDistance: 8.0f,
            IsSolidBlock);

        _renderer?.SetSelection(_selection is { } hit
            ? new VoxelSelectionBox(hit.Block.X, hit.Block.Y, hit.Block.Z)
            : null);
    }

    private void BreakSelectedBlock()
    {
        if (_selection is not { } hit)
        {
            return;
        }

        SetBlock(hit.Block, BlockIds.Air);
        _editCount++;
        RebuildWorldLighting();
        RebuildChunkMesh();
        UpdateSelection();
    }

    private void PlaceSelectedBlock()
    {
        if (_selection is not { } hit || !IsLoadedBlock(hit.Adjacent))
        {
            return;
        }

        if (GetBlock(hit.Adjacent) != BlockIds.Air || WouldBlockIntersectPlayer(hit.Adjacent))
        {
            return;
        }

        SetBlock(hit.Adjacent, _hotbarBlocks[_selectedHotbarSlot]);
        _editCount++;
        RebuildWorldLighting();
        RebuildChunkMesh();
        UpdateSelection();
    }

    private void RebuildChunkMesh()
    {
        ChunkMeshData combinedMesh = new();

        foreach (Chunk chunk in _chunks.Values)
        {
            combinedMesh.Append(_mesher.BuildMesh(chunk, _blocks, IsOpaqueBlock, GetSunlight));
        }

        _renderer?.LoadChunkMesh(VoxelMeshConverter.ToRenderMesh(combinedMesh, _chunks.Count));
    }

    private string GetInteractionDebugText()
    {
        string selectedBlockName = _blocks.Get(_hotbarBlocks[_selectedHotbarSlot]).Name.Replace("sharpengine:", string.Empty, StringComparison.Ordinal);
        string selectionText = _selection is { } hit
            ? $"{hit.Block.X},{hit.Block.Y},{hit.Block.Z}"
            : "NONE";

        string motionText = _player.IsFlying ? "FLY" : _player.IsSwimming ? "SWIM" : _player.IsCrouching ? "CROUCH" : _player.IsGrounded ? "GROUND" : "AIR";
        string worldName = _worldSaveStore?.Metadata.WorldName ?? "NONE";
        string worldId = _worldSaveStore?.WorldId ?? "NONE";
        return $"SEL: {selectionText}\nHOTBAR: {_selectedHotbarSlot + 1}/{selectedBlockName}  EDITS: {_editCount}\nCHUNKS: {_chunks.Count}  RADIUS: {_loadedRadius}  LOADS: {_streamedChunkLoads}  UNLOADS: {_streamedChunkUnloads}  SAVED: {_savedChunkCount}\nMOTION: {motionText}\nVEL: {_player.Velocity.X:0.0},{_player.Velocity.Y:0.0},{_player.Velocity.Z:0.0}\nWORLD: {worldName}  ID: {worldId}\nF TOGGLE FLY  F5 SAVE WORLD  F9 NEW WORLD\nSTATUS: {_worldUiStatus}";
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
                    if (y < 0)
                    {
                        return true;
                    }

                    if (y >= Chunk.Height)
                    {
                        continue;
                    }

                    if (IsSolidBlock(new BlockPosition(x, y, z)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool WouldBlockIntersectPlayer(BlockPosition block)
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
        int groundY = FindHighestSolidBlock(worldX: 8, worldZ: 12);
        NumericsVector3 spawn = new(8.5f, groundY + 1.0f + _player.HalfExtents.Y, 12.5f);
        _player.Teleport(spawn);
    }

    private void LoadInitialChunks(int renderRadius)
    {
        _loadedRadius = renderRadius;
        _streamingCenter = null;
        EnsureChunksAround(new ChunkPosition(0, 0));
    }

    private void UpdateLoadedChunksForPlayer()
    {
        ChunkPosition playerChunk = new BlockPosition(
            (int)MathF.Floor(_player.Position.X),
            0,
            (int)MathF.Floor(_player.Position.Z)).ToChunkPosition();

        if (EnsureChunksAround(playerChunk))
        {
            RebuildWorldLighting();
            RebuildChunkMesh();
            _worldUiStatus = $"STREAMED AROUND {playerChunk.X},{playerChunk.Z}";
        }
    }

    private bool EnsureChunksAround(ChunkPosition center)
    {
        if (_terrainGenerator is null)
        {
            return false;
        }

        if (_streamingCenter == center && _chunks.Count > 0)
        {
            return false;
        }

        HashSet<ChunkPosition> wanted = [];
        for (int chunkZ = center.Z - _loadedRadius; chunkZ <= center.Z + _loadedRadius; chunkZ++)
        {
            for (int chunkX = center.X - _loadedRadius; chunkX <= center.X + _loadedRadius; chunkX++)
            {
                wanted.Add(new ChunkPosition(chunkX, chunkZ));
            }
        }

        bool changed = false;
        foreach (ChunkPosition position in _chunks.Keys.ToArray())
        {
            if (wanted.Contains(position))
            {
                continue;
            }

            _chunks.Remove(position);
            _streamedChunkUnloads++;
            changed = true;
        }

        foreach (ChunkPosition position in wanted)
        {
            if (_chunks.ContainsKey(position))
            {
                continue;
            }

            _chunks[position] = _worldSaveStore?.TryLoadChunk(position) ?? _terrainGenerator.GenerateChunk(position);
            _streamedChunkLoads++;
            changed = true;
        }

        _streamingCenter = center;
        return changed;
    }

    private void SaveLoadedWorld()
    {
        if (_worldSaveStore is null)
        {
            _worldUiStatus = "SAVE UNAVAILABLE";
            return;
        }

        _worldSaveStore.SaveChunks(_chunks.Values);
        _savedChunkCount = _worldSaveStore.SavedChunkCount;
        _worldUiStatus = $"SAVED {_savedChunkCount} CHUNKS";
    }

    private void CreateNewWorld()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int seed = CreateNewWorldSeed();
        TerrainGeneratorSettings settings = CreateTerrainGeneratorSettings(seed, waterLevel: 4);
        string worldId = $"world-{now:yyyyMMdd-HHmmss-fffffff}";
        _worldSaveStore = WorldSaveStore.OpenOrCreate(
            Path.Combine(GetWorldsRootPath(), worldId),
            $"World {now:HHmmss}",
            settings);
        _terrainGenerator = new TerrainGenerator(settings);
        _chunks.Clear();
        _editCount = 0;
        _streamedChunkLoads = 0;
        _streamedChunkUnloads = 0;
        _streamingCenter = null;
        _savedChunkCount = _worldSaveStore.SavedChunkCount;
        LoadInitialChunks(_loadedRadius == 0 ? DefaultRenderRadius : _loadedRadius);
        RebuildWorldLighting();
        MovePlayerToSpawn();
        SyncCameraToPlayer();
        RebuildChunkMesh();
        UpdateSelection();
        _worldUiStatus = $"CREATED {worldId}";
    }

    private void RebuildWorldLighting()
    {
        foreach (Chunk chunk in _chunks.Values)
        {
            _sunlightCalculator.RebuildSunlight(chunk, _blocks);
        }
    }

    private static TerrainGeneratorSettings CreateTerrainGeneratorSettings(int seed, int waterLevel)
    {
        return new TerrainGeneratorSettings(
            seed,
            waterLevel,
            new TerrainBlockPalette(
                BlockIds.Air,
                BlockIds.Grass,
                BlockIds.Dirt,
                BlockIds.Stone,
                BlockIds.Sand,
                BlockIds.Log,
                BlockIds.Leaves));
    }

    private static string GetDefaultWorldPath()
    {
        return Path.Combine(GetWorldsRootPath(), "dev-world");
    }

    private static string GetWorldsRootPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SharpEngine",
            "worlds");
    }

    private static int CreateNewWorldSeed()
    {
        return HashCode.Combine(DateTimeOffset.UtcNow.UtcTicks, Environment.TickCount);
    }

    private bool IsLoadedBlock(BlockPosition position)
    {
        return position.Y is >= 0 and < Chunk.Height && _chunks.ContainsKey(position.ToChunkPosition());
    }

    private ushort GetBlock(BlockPosition position)
    {
        if (position.Y is < 0 or >= Chunk.Height || !_chunks.TryGetValue(position.ToChunkPosition(), out Chunk? chunk))
        {
            return BlockIds.Air;
        }

        return chunk.GetBlock(position.ToLocalBlockPosition());
    }

    private byte GetSunlight(BlockPosition position)
    {
        if (position.Y >= Chunk.Height)
        {
            return Chunk.MaxLightLevel;
        }

        if (position.Y < 0 || !_chunks.TryGetValue(position.ToChunkPosition(), out Chunk? chunk))
        {
            return 0;
        }

        return chunk.GetSunlight(position.ToLocalBlockPosition());
    }

    private void SetBlock(BlockPosition position, ushort blockId)
    {
        if (position.Y is < 0 or >= Chunk.Height || !_chunks.TryGetValue(position.ToChunkPosition(), out Chunk? chunk))
        {
            return;
        }

        chunk.SetBlock(position.ToLocalBlockPosition(), blockId);
        _worldSaveStore?.SaveChunk(chunk);
        _savedChunkCount = _worldSaveStore?.SavedChunkCount ?? 0;
    }

    private bool IsSolidBlock(BlockPosition position)
    {
        return _blocks.Get(GetBlock(position)).IsSolid;
    }

    private bool IsOpaqueBlock(BlockPosition position)
    {
        return _blocks.Get(GetBlock(position)).IsOpaque;
    }

    private int FindHighestSolidBlock(int worldX, int worldZ)
    {
        for (int y = Chunk.Height - 1; y >= 0; y--)
        {
            if (IsSolidBlock(new BlockPosition(worldX, y, worldZ)))
            {
                return y;
            }
        }

        return 0;
    }
}

