# SharpEngine Architecture

SharpEngine is organized as a layered C# voxel game architecture. The main rule is that world simulation must not depend on rendering or platform code. Rendering can observe world state, but the world must remain testable without a GPU or native window.

## Proposed Solution Layout

```text
SharpEngine.sln
src/
  SharpEngine.Core/
  SharpEngine.Platform/
  SharpEngine.Rendering/
  SharpEngine.World/
  SharpEngine.Gameplay/
  SharpEngine.Client/
  SharpEngine.Server/
  SharpEngine.Tools/
tests/
  SharpEngine.Core.Tests/
  SharpEngine.World.Tests/
```

## Project Responsibilities

### SharpEngine.Core

Shared low-level code with no game-specific assumptions.

Responsibilities:

- Logging interfaces and default logger.
- Time abstractions.
- Configuration loading.
- Result/error helpers.
- Basic collections or pooling utilities.
- Serialization primitives shared by world and networking.
- Deterministic random helpers.

Dependencies:

- .NET base class library.
- `System.Numerics`.

Must not depend on:

- Rendering.
- Windowing.
- Client project.
- Server project.

### SharpEngine.Platform

Native platform boundary for windowing, input, audio, and filesystem concerns.

Responsibilities:

- Window creation.
- Keyboard, mouse, and gamepad input.
- Mouse capture.
- Clipboard.
- Audio device abstraction.
- Platform paths.
- Timing integration where required by the windowing layer.

Dependencies:

- `SharpEngine.Core`.
- Windowing/input library such as OpenTK or Silk.NET.

### SharpEngine.Rendering

GPU-facing rendering code.

Responsibilities:

- Graphics device abstraction.
- Shader compilation/loading.
- Buffer, texture, sampler, and pipeline management.
- Mesh upload and disposal.
- Camera matrices.
- Chunk render batching.
- Debug drawing.
- Render statistics.

Dependencies:

- `SharpEngine.Core`.
- `SharpEngine.Platform`.
- Graphics backend package.

Must not own:

- Authoritative block data.
- Gameplay simulation.
- Save files.

### SharpEngine.World

Authoritative world data and simulation primitives.

Responsibilities:

- Block registry.
- Block states and block properties.
- Chunk coordinates.
- Chunk storage.
- Chunk generation.
- Chunk serialization.
- Terrain generation.
- Lighting data and propagation.
- Block ticking.
- Spatial queries and raycasts.
- Save/load format.

Dependencies:

- `SharpEngine.Core`.

Must not depend on:

- Rendering.
- Platform.
- Client.

### SharpEngine.Gameplay

Rules that operate on the world.

Responsibilities:

- Player controller simulation.
- Inventory.
- Items.
- Crafting.
- Tools.
- Entity model.
- Combat and health.
- Mob AI.
- Interaction rules.
- Game modes.

Dependencies:

- `SharpEngine.Core`.
- `SharpEngine.World`.

### SharpEngine.Client

The playable game executable.

Responsibilities:

- Application startup.
- Game loop.
- Client state management.
- Input mapping.
- Camera/player control binding.
- UI screens.
- Asset loading.
- Client-side prediction later.
- Local single-player server hosting later.

Dependencies:

- All runtime projects except tools.

### SharpEngine.Server

Dedicated authoritative server executable.

Responsibilities:

- Headless world simulation.
- Player sessions.
- Networking.
- Chunk streaming.
- Server-side persistence.
- Commands and admin tools.

Dependencies:

- `SharpEngine.Core`.
- `SharpEngine.World`.
- `SharpEngine.Gameplay`.

Must not depend on:

- Rendering.
- Platform windowing.

### SharpEngine.Tools

Developer and content tools.

Responsibilities:

- Asset packing.
- Save inspection.
- Chunk visualization exports.
- Content validation.
- Benchmark runners.
- Migration utilities.

Dependencies:

- Depends on whichever runtime libraries are needed for each tool.

## Runtime Architecture

The client runtime is divided into five major loops:

1. Input collection.
2. Fixed-step simulation.
3. Asynchronous world work.
4. Render preparation.
5. GPU rendering.

The server runtime uses only:

1. Network input.
2. Fixed-step simulation.
3. Asynchronous world work.
4. Persistence.
5. Network output.

## World Model

### Coordinates

Use explicit coordinate types to avoid mixing spaces:

- `BlockPosition`: integer global block coordinate.
- `ChunkPosition`: integer chunk coordinate.
- `LocalBlockPosition`: block coordinate inside a chunk or chunk section.
- `RegionPosition`: save-file region coordinate.

### Chunks

Initial recommendation:

- Horizontal chunk size: `16 x 16`.
- Vertical storage: chunk sections of `16 x 16 x 16`.
- First playable world can clamp vertical range while preserving section-based storage.

This keeps early implementation simple while leaving room for expanded world height.

### Blocks

Blocks should be registered by stable numeric IDs at runtime.

Block data should include:

- Name.
- Solidity.
- Opacity.
- Light emission.
- Collision shape.
- Render shape.
- Texture references.
- Hardness.
- Drops.
- Tick behavior flag.

Block instances in chunks should store compact block state IDs rather than large objects.

### Items

Items are separate from blocks, even when an item places a block.

Item data should include:

- Name.
- Max stack size.
- Use behavior.
- Tool type.
- Durability.
- Optional placed block state.

## Rendering Architecture

### Chunk Meshing

Start with culled-face meshing:

- Emit only faces adjacent to air or transparent blocks.
- Emit vertex position, normal, UV, light, ambient occlusion, and material index.
- Rebuild affected chunk meshes after block edits.

Later optimization:

- Greedy meshing for opaque block faces.
- Separate meshes for opaque, cutout, translucent, and fluid geometry.
- Mesh worker threads.
- Persistent mapped buffers or staging upload queues if needed.

### Materials and Textures

Initial recommendation:

- Use a texture atlas for speed of implementation.
- Move to texture arrays if atlas bleeding or animation complexity becomes costly.

Render passes:

- Sky.
- Opaque chunks.
- Cutout chunks.
- Entities.
- Translucent chunks and fluids.
- Particles.
- UI.

## Simulation Architecture

### Fixed Step

World and gameplay simulation should run at a fixed tick rate, likely 20 ticks per second for Minecraft-like behavior.

Rendering can run independently at display frame rate with interpolation where useful.

### Asynchronous Work

Background jobs should handle:

- Chunk generation.
- Chunk mesh generation.
- Save compression and disk writes.
- Lighting rebuilds when they become expensive.

Background jobs must not mutate authoritative world state directly. They should produce results that are applied on the simulation thread or through a controlled command queue.

### Commands

World edits should flow through commands:

- Place block.
- Break block.
- Set block.
- Spawn entity.
- Damage entity.

This makes undo/debugging easier and prepares the design for multiplayer validation.

## Persistence Architecture

Use a versioned save format from the first implementation.

Recommended initial layout:

```text
worlds/
  MyWorld/
    world.json
    player.dat
    regions/
      r.0.0.srg
```

`world.json` should contain:

- Save format version.
- Game version.
- Seed.
- World name.
- Created timestamp.
- Last played timestamp.
- Generator settings.

Region files can be introduced after chunk-per-file persistence if that helps early iteration. The important rule is that chunk payloads are versioned and migratable.

## Multiplayer Architecture

Single-player should eventually run an internal server rather than letting the client own all simulation forever.

Authoritative server responsibilities:

- Validate player movement.
- Validate block edits.
- Own entity simulation.
- Own save files.
- Stream chunks to clients.
- Broadcast world changes.

Client responsibilities:

- Input.
- Rendering.
- Prediction where necessary.
- UI.
- Local interpolation.

This boundary should influence early code even before networking exists.

## Testing Strategy

Fast automated tests should cover:

- Coordinate conversions.
- Chunk indexing.
- Block registry behavior.
- Chunk serialization.
- Terrain generation determinism.
- Raycasts.
- Lighting propagation.
- Inventory rules.
- Crafting recipes.

Manual/debug tests should cover:

- Chunk streaming.
- Mesh rebuild correctness.
- Collision edge cases.
- Save/load flows.
- Performance under large render distances.

## Near-Term Decisions

Before scaffolding code, decide:

- Rendering backend: OpenTK/OpenGL, Silk.NET/OpenGL, Silk.NET/Vulkan, or another stack.
- Target platforms for the first playable build.
- Initial world height.
- Whether single-player should immediately route through a local server abstraction.
- Asset style and texture resolution.
- Whether to prioritize creative mode or survival mode first.
