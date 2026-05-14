# Decision 0001: Initial Rendering Backend

## Status

Accepted for Milestone 1.

## Context

SharpEngine needs an early native window, input path, render loop, and simple 3D scene before voxel chunk rendering can begin. The engine should keep simulation decoupled from rendering and avoid committing to a high-complexity backend before the world systems exist.

## Decision

Use OpenTK `4.9.4` with OpenGL `3.3 Core` for the first rendering backend.

OpenTK is used only in the platform and rendering layers:

- `SharpEngine.Platform` owns the native window, input polling, and loop callbacks.
- `SharpEngine.Rendering` owns OpenGL objects, shaders, camera matrices, and draw calls.
- `SharpEngine.World` remains independent of OpenTK and GPU APIs.

## Consequences

- Milestone 1 can focus on a working window, camera, and render loop with minimal backend code.
- The renderer is not yet abstract enough for multiple graphics APIs.
- Vulkan or another backend can still be evaluated later because world simulation does not depend on rendering.
