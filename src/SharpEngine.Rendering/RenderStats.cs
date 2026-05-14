namespace SharpEngine.Rendering;

public readonly record struct RenderStats(
    int DrawCalls,
    int VisibleChunks,
    int UploadedMeshes);

