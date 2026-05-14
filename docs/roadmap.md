# SharpEngine Roadmap

SharpEngine is planned as a C# voxel sandbox inspired by Minecraft. The goal is to build a playable, moddable, persistent world with survival gameplay, creative building, procedural terrain, and eventually multiplayer.

This roadmap favors a vertical-slice approach: get a narrow playable loop working early, then deepen systems without throwing away the foundation.

## Guiding Priorities

- Keep the world model deterministic and testable.
- Separate simulation from rendering from the beginning.
- Make data formats versioned early, even if the first implementation is simple.
- Favor simple working systems before advanced optimizations.
- Build tools and debug views alongside engine features.
- Treat multiplayer as a future architectural constraint, even before implementing it.

## Milestone 0: Project Foundation

Goal: create the basic repository structure and development workflow.

Deliverables:

- .NET solution and project layout.
- Client executable.
- Core library for shared utilities.
- Rendering/platform abstraction.
- World library for block, chunk, and terrain logic.
- Test projects for non-rendering systems.
- Logging, config loading, and basic diagnostics.
- Initial architecture documentation.

Exit criteria:

- `dotnet build` succeeds.
- `dotnet test` runs at least one placeholder test.
- Client project starts and exits cleanly.

## Milestone 1: Window, Loop, and Camera

Goal: open a native window and render a simple scene with interactive camera controls.

Deliverables:

- Fixed/update plus render game loop.
- Keyboard and mouse input.
- First-person debug camera.
- GPU shader loading.
- Basic mesh and texture abstractions.
- Debug overlay for frame time, camera position, and draw statistics.

Exit criteria:

- A user can move around a simple 3D test scene.
- Frame timing and input are stable enough for development.

## Milestone 2: First Voxel Chunk

Goal: render a small voxel world chunk.

Deliverables:

- Block ID registry.
- Chunk coordinate system.
- Chunk block storage.
- Basic cube-face mesh generation.
- Face culling between adjacent solid blocks.
- Texture atlas or texture array support.
- Simple block selection raycast.

Exit criteria:

- One generated chunk renders with multiple block types.
- Hidden internal faces are not emitted.
- The player can point at a block and identify it.

## Milestone 3: Editable World

Goal: support placing and breaking blocks in a local chunked world.

Deliverables:

- Block breaking and placement.
- Dirty chunk tracking.
- Chunk remeshing queue.
- Neighbor chunk remesh invalidation at chunk borders.
- Simple hotbar.
- Basic crosshair and selected block outline.

Exit criteria:

- A user can place and break blocks without restarting.
- Meshes update correctly across chunk boundaries.

## Milestone 4: Player Physics

Goal: replace the debug camera with a playable first-person controller.

Deliverables:

- Player capsule or AABB collider.
- Gravity, jumping, walking, sprinting, crouching, and swimming placeholder.
- Voxel collision broadphase and narrowphase.
- Step-up behavior.
- Reach distance and interaction rules.

Exit criteria:

- The player can walk on terrain, jump, collide with blocks, and interact with blocks reliably.

## Milestone 5: Terrain Generation

Goal: generate an explorable procedural world.

Deliverables:

- Seeded world generation.
- Heightmap terrain.
- Basic biomes.
- Dirt, grass, stone, sand, water, ores, and trees.
- Spawn selection.
- Chunk generation worker queue.
- Configurable render distance.

Exit criteria:

- A user can create a world from a seed and explore generated terrain.
- Nearby chunks stream in and out without blocking the main thread noticeably.

## Milestone 6: Persistence

Goal: save and load world edits.

Deliverables:

- World metadata file.
- Versioned chunk save format.
- Chunk compression.
- Dirty chunk flush policy.
- Autosave.
- World list and create/load flow.

Exit criteria:

- Block edits persist after closing and reopening the game.
- Save files can report their format version.

## Milestone 7: Lighting

Goal: make the world readable and atmospheric.

Deliverables:

- Sunlight values.
- Block light values.
- Light propagation.
- Light removal and update propagation after block edits.
- Per-vertex ambient occlusion.
- Day/night cycle.
- Basic emissive blocks.

Exit criteria:

- Terrain has stable sunlight.
- Torches or equivalent light-emitting blocks illuminate nearby space.
- Breaking and placing blocks updates lighting.

## Milestone 8: Inventory, Items, and Crafting

Goal: add the core survival interaction loop.

Deliverables:

- Item registry.
- Item stacks.
- Hotbar and inventory UI.
- Block drops.
- Crafting recipes.
- Furnace/smelting placeholder.
- Chests or basic storage containers.
- Tool effectiveness and durability.

Exit criteria:

- A user can gather blocks/items, craft simple items, store items, and use tools.

## Milestone 9: Entities and Combat

Goal: support world objects and living actors beyond the player.

Deliverables:

- Entity system.
- Dropped item entities.
- Health and damage.
- Simple hostile and passive mobs.
- Spawn rules.
- Basic pathfinding.
- Projectiles placeholder.
- Entity persistence.

Exit criteria:

- Mobs spawn, move, can be damaged, and persist or despawn by clear rules.

## Milestone 10: Fluids, Redstone-Like Logic, and World Simulation

Goal: add dynamic block behavior.

Deliverables:

- Water and lava flow.
- Crop growth.
- Fire placeholder.
- Doors, trapdoors, buttons, levers, pressure plates.
- Simple signal/power system.
- Scheduled block ticks.
- Simulation budget controls.

Exit criteria:

- Dynamic blocks update consistently without overwhelming frame time.

## Milestone 11: Multiplayer Foundation

Goal: split the game into client and authoritative server simulation.

Deliverables:

- Dedicated server executable.
- Client/server protocol.
- Login/session handshake.
- Chunk streaming over the network.
- Block update replication.
- Player movement replication.
- Entity replication.
- Server-side save loading.

Exit criteria:

- Two clients can connect to a local server, see each other, and edit the same world.

## Milestone 12: Modding and Data-Driven Content

Goal: make content extensible without recompiling the engine.

Deliverables:

- Data-driven block definitions.
- Data-driven item definitions.
- Recipe data files.
- Texture pack structure.
- Script or plugin boundary evaluation.
- Mod loading order.
- Content validation diagnostics.

Exit criteria:

- A new simple block, item, and recipe can be added through data files.

## Milestone 13: Polish and Release Packaging

Goal: turn the prototype into a distributable game.

Deliverables:

- Main menu.
- Settings menu.
- Keybinding UI.
- Audio and music.
- Particles.
- Weather.
- Screenshots.
- Crash logs.
- Packaging scripts.
- Performance profiling pass.

Exit criteria:

- A user can install, launch, create a world, play, save, quit, and reload without developer tools.

## Technical Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Chunk meshing becomes CPU-bound | Poor streaming and editing performance | Start simple, then add greedy meshing, worker queues, and mesh cache invalidation. |
| Lighting updates are complex | Visual bugs and slow block edits | Isolate lighting into a tested world module before coupling it to rendering. |
| Save format changes break worlds | Lost user data | Version saves from the first implementation and write migration tests. |
| Multiplayer requires major rewrites | Late architecture churn | Keep simulation independent from rendering and avoid client-only world authority. |
| Data-driven content becomes inconsistent | Hard-to-debug content errors | Add schema validation and content diagnostics early. |

## Open Questions

- Should the renderer start with OpenTK/OpenGL, Silk.NET/OpenGL, Silk.NET/Vulkan, or a higher-level framework?
- Should the first target be Windows-only or cross-platform from the first milestone?
- Should the visual style be classic blocky textures, higher-resolution stylized textures, or programmer-art until systems stabilize?
- Should world height use fixed vertical bounds initially, or chunk sections that can expand vertically later?
- Should multiplayer be designed for cooperative small servers only, or eventually public internet servers?
- How close should gameplay stay to Minecraft versus intentionally diverging into SharpEngine-specific mechanics?
