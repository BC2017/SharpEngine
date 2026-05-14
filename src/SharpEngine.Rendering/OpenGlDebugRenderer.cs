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
    private int _height;
    private int _width;
    private bool _disposed;

    public OpenGlDebugRenderer(int width, int height)
    {
        _width = width;
        _height = height;

        _shaderProgram = CreateShaderProgram(VertexShaderSource, FragmentShaderSource);
        _modelUniform = GL.GetUniformLocation(_shaderProgram, "uModel");
        _viewUniform = GL.GetUniformLocation(_shaderProgram, "uView");
        _projectionUniform = GL.GetUniformLocation(_shaderProgram, "uProjection");

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, CubeVertices.Length * sizeof(float), CubeVertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, normalized: false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, normalized: false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        GL.Enable(EnableCap.DepthTest);
        Resize(width, height);
    }

    public RenderStats Stats { get; private set; }

    public void Resize(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        GL.Viewport(0, 0, _width, _height);
    }

    public void RenderFrame(DebugCamera camera, TimeSpan totalTime)
    {
        float aspectRatio = (float)_width / _height;
        Matrix4 model =
            Matrix4.CreateRotationY((float)totalTime.TotalSeconds * 0.65f) *
            Matrix4.CreateRotationX((float)totalTime.TotalSeconds * 0.2f);
        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix(aspectRatio);

        GL.ClearColor(0.08f, 0.11f, 0.14f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_modelUniform, transpose: false, ref model);
        GL.UniformMatrix4(_viewUniform, transpose: false, ref view);
        GL.UniformMatrix4(_projectionUniform, transpose: false, ref projection);

        GL.BindVertexArray(_vertexArray);
        GL.DrawArrays(PrimitiveType.Triangles, 0, CubeVertices.Length / 6);
        GL.BindVertexArray(0);

        Stats = new RenderStats(DrawCalls: 1, VisibleChunks: 0, UploadedMeshes: 0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
        _disposed = true;
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
        layout (location = 1) in vec3 aColor;

        out vec3 vColor;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        void main()
        {
            vColor = aColor;
            gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec3 vColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(vColor, 1.0);
        }
        """;

    private static readonly float[] CubeVertices =
    [
        // Front
        -0.5f, -0.5f,  0.5f, 0.85f, 0.28f, 0.22f,
         0.5f, -0.5f,  0.5f, 0.85f, 0.28f, 0.22f,
         0.5f,  0.5f,  0.5f, 0.85f, 0.28f, 0.22f,
         0.5f,  0.5f,  0.5f, 0.85f, 0.28f, 0.22f,
        -0.5f,  0.5f,  0.5f, 0.85f, 0.28f, 0.22f,
        -0.5f, -0.5f,  0.5f, 0.85f, 0.28f, 0.22f,

        // Back
        -0.5f, -0.5f, -0.5f, 0.20f, 0.55f, 0.82f,
        -0.5f,  0.5f, -0.5f, 0.20f, 0.55f, 0.82f,
         0.5f,  0.5f, -0.5f, 0.20f, 0.55f, 0.82f,
         0.5f,  0.5f, -0.5f, 0.20f, 0.55f, 0.82f,
         0.5f, -0.5f, -0.5f, 0.20f, 0.55f, 0.82f,
        -0.5f, -0.5f, -0.5f, 0.20f, 0.55f, 0.82f,

        // Left
        -0.5f,  0.5f,  0.5f, 0.24f, 0.68f, 0.38f,
        -0.5f,  0.5f, -0.5f, 0.24f, 0.68f, 0.38f,
        -0.5f, -0.5f, -0.5f, 0.24f, 0.68f, 0.38f,
        -0.5f, -0.5f, -0.5f, 0.24f, 0.68f, 0.38f,
        -0.5f, -0.5f,  0.5f, 0.24f, 0.68f, 0.38f,
        -0.5f,  0.5f,  0.5f, 0.24f, 0.68f, 0.38f,

        // Right
         0.5f,  0.5f,  0.5f, 0.94f, 0.72f, 0.26f,
         0.5f, -0.5f, -0.5f, 0.94f, 0.72f, 0.26f,
         0.5f,  0.5f, -0.5f, 0.94f, 0.72f, 0.26f,
         0.5f, -0.5f, -0.5f, 0.94f, 0.72f, 0.26f,
         0.5f,  0.5f,  0.5f, 0.94f, 0.72f, 0.26f,
         0.5f, -0.5f,  0.5f, 0.94f, 0.72f, 0.26f,

        // Top
        -0.5f,  0.5f, -0.5f, 0.73f, 0.36f, 0.78f,
        -0.5f,  0.5f,  0.5f, 0.73f, 0.36f, 0.78f,
         0.5f,  0.5f,  0.5f, 0.73f, 0.36f, 0.78f,
         0.5f,  0.5f,  0.5f, 0.73f, 0.36f, 0.78f,
         0.5f,  0.5f, -0.5f, 0.73f, 0.36f, 0.78f,
        -0.5f,  0.5f, -0.5f, 0.73f, 0.36f, 0.78f,

        // Bottom
        -0.5f, -0.5f, -0.5f, 0.32f, 0.36f, 0.43f,
         0.5f, -0.5f,  0.5f, 0.32f, 0.36f, 0.43f,
        -0.5f, -0.5f,  0.5f, 0.32f, 0.36f, 0.43f,
         0.5f, -0.5f,  0.5f, 0.32f, 0.36f, 0.43f,
        -0.5f, -0.5f, -0.5f, 0.32f, 0.36f, 0.43f,
         0.5f, -0.5f, -0.5f, 0.32f, 0.36f, 0.43f
    ];
}

