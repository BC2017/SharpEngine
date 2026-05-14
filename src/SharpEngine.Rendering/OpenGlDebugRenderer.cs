using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpEngine.Rendering;

public sealed class OpenGlDebugRenderer : IRenderer
{
    private readonly int _shaderProgram;
    private readonly int _vertexArray;
    private readonly int _vertexBuffer;
    private readonly int _modelUniform;
    private readonly int _viewUniform;
    private readonly int _projectionUniform;
    private readonly int _textureArray;
    private readonly DebugOverlayRenderer _debugOverlayRenderer;
    private readonly int _selectionShaderProgram;
    private readonly int _selectionVertexArray;
    private readonly int _selectionVertexBuffer;
    private readonly int _selectionModelUniform;
    private readonly int _selectionProjectionUniform;
    private readonly int _selectionViewUniform;
    private int _height;
    private int _indexBuffer;
    private int _indexCount;
    private int _width;
    private bool _disposed;
    private bool _hasUploadedMesh;
    private VoxelSelectionBox? _selection;

    public OpenGlDebugRenderer(int width, int height)
    {
        _width = width;
        _height = height;

        _shaderProgram = CreateShaderProgram(VertexShaderSource, FragmentShaderSource);
        _modelUniform = GL.GetUniformLocation(_shaderProgram, "uModel");
        _viewUniform = GL.GetUniformLocation(_shaderProgram, "uView");
        _projectionUniform = GL.GetUniformLocation(_shaderProgram, "uProjection");
        _selectionShaderProgram = CreateShaderProgram(SelectionVertexShaderSource, SelectionFragmentShaderSource);
        _selectionModelUniform = GL.GetUniformLocation(_selectionShaderProgram, "uModel");
        _selectionViewUniform = GL.GetUniformLocation(_selectionShaderProgram, "uView");
        _selectionProjectionUniform = GL.GetUniformLocation(_selectionShaderProgram, "uProjection");

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        _textureArray = CreateDebugTextureArray();
        _debugOverlayRenderer = new DebugOverlayRenderer();
        _selectionVertexArray = GL.GenVertexArray();
        _selectionVertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);

        int stride = 9 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, normalized: false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, normalized: false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, normalized: false, stride, 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, normalized: false, stride, 8 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        GL.BindVertexArray(_selectionVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _selectionVertexBuffer);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, normalized: false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        GL.Enable(EnableCap.DepthTest);
        Resize(width, height);
    }

    public RenderStats Stats { get; private set; }

    public void LoadChunkMesh(VoxelRenderMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        float[] vertices = new float[mesh.Vertices.Count * 9];
        int cursor = 0;

        foreach (VoxelRenderVertex vertex in mesh.Vertices)
        {
            vertices[cursor++] = vertex.X;
            vertices[cursor++] = vertex.Y;
            vertices[cursor++] = vertex.Z;
            vertices[cursor++] = vertex.NormalX;
            vertices[cursor++] = vertex.NormalY;
            vertices[cursor++] = vertex.NormalZ;
            vertices[cursor++] = vertex.U;
            vertices[cursor++] = vertex.V;
            vertices[cursor++] = vertex.TextureIndex;
        }

        uint[] indices = [.. mesh.Indices];

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);

        _indexCount = indices.Length;
        _hasUploadedMesh = true;
        Stats = Stats with
        {
            UploadedMeshes = Stats.UploadedMeshes + 1,
            VertexCount = mesh.Vertices.Count,
            IndexCount = mesh.Indices.Count,
            TriangleCount = mesh.Indices.Count / 3,
            FaceCount = mesh.Indices.Count / 6
        };
    }

    public void Resize(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        GL.Viewport(0, 0, _width, _height);
    }

    public void SetSelection(VoxelSelectionBox? selection)
    {
        _selection = selection;
    }

    public void RenderFrame(DebugCamera camera, TimeSpan totalTime, DebugOverlaySnapshot debugOverlay)
    {
        float aspectRatio = (float)_width / _height;
        Matrix4 model = Matrix4.Identity;
        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix(aspectRatio);

        GL.ClearColor(0.51f, 0.68f, 0.92f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (!_hasUploadedMesh || _indexCount == 0)
        {
            Stats = Stats with { DrawCalls = 0, VisibleChunks = 0 };
            RenderDebugOverlay(debugOverlay);
            return;
        }

        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_modelUniform, transpose: false, ref model);
        GL.UniformMatrix4(_viewUniform, transpose: false, ref view);
        GL.UniformMatrix4(_projectionUniform, transpose: false, ref projection);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2DArray, _textureArray);

        GL.BindVertexArray(_vertexArray);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);

        Stats = Stats with { DrawCalls = 1, VisibleChunks = 1 };
        RenderSelection(camera);
        RenderDebugOverlay(debugOverlay);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteBuffer(_selectionVertexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteVertexArray(_selectionVertexArray);
        GL.DeleteTexture(_textureArray);
        GL.DeleteProgram(_shaderProgram);
        GL.DeleteProgram(_selectionShaderProgram);
        _debugOverlayRenderer.Dispose();
        _disposed = true;
    }

    private void RenderSelection(DebugCamera camera)
    {
        if (_selection is not { } selection)
        {
            return;
        }

        float minX = selection.X - 0.01f;
        float minY = selection.Y - 0.01f;
        float minZ = selection.Z - 0.01f;
        float maxX = selection.X + 1.01f;
        float maxY = selection.Y + 1.01f;
        float maxZ = selection.Z + 1.01f;

        float[] lines =
        [
            minX, minY, minZ, maxX, minY, minZ,
            maxX, minY, minZ, maxX, minY, maxZ,
            maxX, minY, maxZ, minX, minY, maxZ,
            minX, minY, maxZ, minX, minY, minZ,

            minX, maxY, minZ, maxX, maxY, minZ,
            maxX, maxY, minZ, maxX, maxY, maxZ,
            maxX, maxY, maxZ, minX, maxY, maxZ,
            minX, maxY, maxZ, minX, maxY, minZ,

            minX, minY, minZ, minX, maxY, minZ,
            maxX, minY, minZ, maxX, maxY, minZ,
            maxX, minY, maxZ, maxX, maxY, maxZ,
            minX, minY, maxZ, minX, maxY, maxZ
        ];

        float aspectRatio = (float)_width / _height;
        Matrix4 model = Matrix4.Identity;
        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix(aspectRatio);

        GL.UseProgram(_selectionShaderProgram);
        GL.UniformMatrix4(_selectionModelUniform, transpose: false, ref model);
        GL.UniformMatrix4(_selectionViewUniform, transpose: false, ref view);
        GL.UniformMatrix4(_selectionProjectionUniform, transpose: false, ref projection);
        GL.LineWidth(2.0f);
        GL.BindVertexArray(_selectionVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _selectionVertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, lines.Length * sizeof(float), lines, BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Lines, 0, lines.Length / 3);
        GL.BindVertexArray(0);

        Stats = Stats with { DrawCalls = Stats.DrawCalls + 1 };
    }

    private void RenderDebugOverlay(DebugOverlaySnapshot debugOverlay)
    {
        if (!debugOverlay.IsVisible)
        {
            return;
        }

        string[] lines =
        [
            $"FPS: {debugOverlay.FramesPerSecond,6:0.0}",
            $"FRAME: {debugOverlay.FrameTimeMilliseconds,5:0.00} MS",
            $"DRAWS: {Stats.DrawCalls}",
            $"TRIS: {Stats.TriangleCount}",
            $"FACES: {Stats.FaceCount}",
            $"VERTS: {Stats.VertexCount}",
            $"INDICES: {Stats.IndexCount}",
            $"CHUNKS: {Stats.VisibleChunks}",
            $"MESH UPLOADS: {Stats.UploadedMeshes}",
            $"TICKS: {debugOverlay.FixedTicks}",
            $"CAM: {debugOverlay.CameraPosition.X:0.0}, {debugOverlay.CameraPosition.Y:0.0}, {debugOverlay.CameraPosition.Z:0.0}",
            debugOverlay.InteractionText
        ];

        int overlayDrawCalls = _debugOverlayRenderer.Draw(lines, _width, _height);
        Stats = Stats with { DrawCalls = Stats.DrawCalls + overlayDrawCalls };
    }

    private static int CreateShaderProgram(string vertexSource, string fragmentSource)
    {
        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
        int program = GL.CreateProgram();

        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
        {
            string log = GL.GetProgramInfoLog(program);
            throw new InvalidOperationException($"Failed to link OpenGL shader program: {log}");
        }

        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status != 0)
        {
            return shader;
        }

        string log = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException($"Failed to compile {type} shader: {log}");
    }

    private const string VertexShaderSource = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;
        layout (location = 3) in float aTextureIndex;

        out vec3 vNormal;
        out vec2 vTexCoord;
        out float vTextureIndex;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        void main()
        {
            vNormal = mat3(uModel) * aNormal;
            vTexCoord = aTexCoord;
            vTextureIndex = aTextureIndex;
            gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec3 vNormal;
        in vec2 vTexCoord;
        in float vTextureIndex;
        out vec4 FragColor;

        uniform sampler2DArray uTextureArray;

        void main()
        {
            vec3 lightDirection = normalize(vec3(0.35, 0.85, 0.25));
            float light = 0.62 + 0.38 * max(dot(normalize(vNormal), lightDirection), 0.0);
            vec4 texel = texture(uTextureArray, vec3(vTexCoord, vTextureIndex));
            FragColor = vec4(texel.rgb * light, texel.a);
        }
        """;

    private const string SelectionVertexShaderSource = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        void main()
        {
            gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
        }
        """;

    private const string SelectionFragmentShaderSource = """
        #version 330 core

        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(0.02, 0.02, 0.02, 1.0);
        }
        """;

    private static int CreateDebugTextureArray()
    {
        const int textureSize = 16;
        const int layerCount = 8;
        byte[] pixels = new byte[textureSize * textureSize * layerCount * 4];

        FillLayer(pixels, textureSize, 0, (0, 0, 0), (0, 0, 0));
        FillLayer(pixels, textureSize, 1, (74, 154, 67), (95, 184, 81));
        FillLayer(pixels, textureSize, 2, (121, 82, 47), (101, 69, 39));
        FillLayer(pixels, textureSize, 3, (121, 125, 128), (97, 101, 105));
        FillLayer(pixels, textureSize, 4, (210, 194, 122), (232, 214, 144));
        FillLayer(pixels, textureSize, 5, (117, 75, 42), (92, 58, 34));
        FillLayer(pixels, textureSize, 6, (63, 116, 52), (49, 94, 42));
        FillLayer(pixels, textureSize, 7, (96, 132, 205), (75, 111, 184));

        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DArray, texture);
        GL.TexImage3D(
            TextureTarget.Texture2DArray,
            level: 0,
            PixelInternalFormat.Rgba8,
            textureSize,
            textureSize,
            layerCount,
            border: 0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            pixels);

        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        return texture;
    }

    private static void FillLayer(
        byte[] pixels,
        int textureSize,
        int layer,
        (byte R, byte G, byte B) baseColor,
        (byte R, byte G, byte B) accentColor)
    {
        int layerOffset = layer * textureSize * textureSize * 4;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool accent = ((x / 4) + (y / 4)) % 2 == 0;
                (byte r, byte g, byte b) = accent ? accentColor : baseColor;
                int offset = layerOffset + ((y * textureSize + x) * 4);

                pixels[offset] = r;
                pixels[offset + 1] = g;
                pixels[offset + 2] = b;
                pixels[offset + 3] = 255;
            }
        }
    }
}

