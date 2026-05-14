using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpEngine.Rendering;

internal sealed class DebugOverlayRenderer : IDisposable
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const float PixelSize = 2.0f;
    private const float LineHeight = 18.0f;

    private readonly int _shaderProgram;
    private readonly int _vertexArray;
    private readonly int _vertexBuffer;
    private readonly int _projectionUniform;
    private bool _disposed;

    public DebugOverlayRenderer()
    {
        _shaderProgram = CreateShaderProgram(VertexShaderSource, FragmentShaderSource);
        _projectionUniform = GL.GetUniformLocation(_shaderProgram, "uProjection");
        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, normalized: false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, normalized: false, 6 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public int Draw(string[] lines, int width, int height)
    {
        List<float> vertices = [];
        float y = 12.0f;

        AddPanel(vertices, 8.0f, 8.0f, 360.0f, 22.0f + (lines.Length * LineHeight));

        foreach (string line in lines)
        {
            AddText(vertices, line.ToUpperInvariant(), 16.0f, y, PixelSize, (0.90f, 0.96f, 1.00f, 1.00f));
            y += LineHeight;
        }

        if (vertices.Count == 0)
        {
            return 0;
        }

        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0.0f, width, height, 0.0f, -1.0f, 1.0f);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.UseProgram(_shaderProgram);
        GL.UniformMatrix4(_projectionUniform, transpose: false, ref projection);

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 6);
        GL.BindVertexArray(0);

        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);

        return 1;
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

    private static void AddText(
        List<float> vertices,
        string text,
        float x,
        float y,
        float scale,
        (float R, float G, float B, float A) color)
    {
        float cursorX = x;

        foreach (char c in text)
        {
            if (c == ' ')
            {
                cursorX += (GlyphWidth + 1) * scale;
                continue;
            }

            if (Glyphs.TryGetValue(c, out string[]? glyph))
            {
                AddGlyph(vertices, glyph, cursorX, y, scale, color);
            }

            cursorX += (GlyphWidth + 1) * scale;
        }
    }

    private static void AddGlyph(
        List<float> vertices,
        string[] glyph,
        float x,
        float y,
        float scale,
        (float R, float G, float B, float A) color)
    {
        for (int row = 0; row < GlyphHeight; row++)
        {
            string bits = glyph[row];

            for (int column = 0; column < GlyphWidth; column++)
            {
                if (bits[column] != '1')
                {
                    continue;
                }

                AddQuad(vertices, x + (column * scale), y + (row * scale), scale, scale, color);
            }
        }
    }

    private static void AddPanel(List<float> vertices, float x, float y, float width, float height)
    {
        AddQuad(vertices, x, y, width, height, (0.02f, 0.03f, 0.04f, 0.72f));
    }

    private static void AddQuad(
        List<float> vertices,
        float x,
        float y,
        float width,
        float height,
        (float R, float G, float B, float A) color)
    {
        AddVertex(vertices, x, y, color);
        AddVertex(vertices, x + width, y, color);
        AddVertex(vertices, x + width, y + height, color);
        AddVertex(vertices, x, y, color);
        AddVertex(vertices, x + width, y + height, color);
        AddVertex(vertices, x, y + height, color);
    }

    private static void AddVertex(List<float> vertices, float x, float y, (float R, float G, float B, float A) color)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(color.R);
        vertices.Add(color.G);
        vertices.Add(color.B);
        vertices.Add(color.A);
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
            throw new InvalidOperationException($"Failed to link debug overlay shader: {GL.GetProgramInfoLog(program)}");
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
        throw new InvalidOperationException($"Failed to compile debug overlay {type} shader: {log}");
    }

    private const string VertexShaderSource = """
        #version 330 core

        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec4 aColor;

        out vec4 vColor;

        uniform mat4 uProjection;

        void main()
        {
            vColor = aColor;
            gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec4 vColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vColor;
        }
        """;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = ["11111", "10001", "10011", "10101", "11001", "10001", "11111"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["11110", "00001", "00001", "11110", "10000", "10000", "11111"],
        ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
        ['4'] = ["10010", "10010", "10010", "11111", "00010", "00010", "00010"],
        ['5'] = ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
        ['6'] = ["01111", "10000", "10000", "11110", "10001", "10001", "01110"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "11110"],
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
        ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
        ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        ['G'] = ["01111", "10000", "10000", "10011", "10001", "10001", "01111"],
        ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        ['W'] = ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
        ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        ['.'] = ["00000", "00000", "00000", "00000", "00000", "01100", "01100"],
        [':'] = ["00000", "01100", "01100", "00000", "01100", "01100", "00000"],
        ['-'] = ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
        [','] = ["00000", "00000", "00000", "00000", "01100", "01100", "01000"],
        ['/'] = ["00001", "00010", "00010", "00100", "01000", "01000", "10000"]
    };
}

