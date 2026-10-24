using System.Numerics;

namespace SharpEngine;

public sealed class RenderContext : IDisposable
{
    private readonly IRenderBackend _backend;
    private readonly HashSet<Texture2D> _textures = [];
    private bool _isDisposed;

    internal RenderContext(IRenderBackend backend)
    {
        _backend = backend;
    }

    public Vector2 OutputSize
    {
        get
        {
            ThrowIfDisposed();
            return _backend.OutputSize;
        }
    }

    public void Clear(RenderColor color)
    {
        ThrowIfDisposed();
        _backend.Clear(color);
    }

    public void FillRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        ValidateRectangle(position, size);
        ThrowIfDisposed();
        _backend.FillRectangle(position, size, color);
    }

    public void DrawRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        ValidateRectangle(position, size);
        ThrowIfDisposed();
        _backend.DrawRectangle(position, size, color);
    }

    public void DrawLine(Vector2 start, Vector2 end, RenderColor color)
    {
        ValidatePoint(start, nameof(start));
        ValidatePoint(end, nameof(end));
        ThrowIfDisposed();
        _backend.DrawLine(start, end, color);
    }

    public void DrawCircle(Vector2 center, float radius, RenderColor color, int segments = 32)
    {
        ValidatePoint(center, nameof(center));
        if (!float.IsFinite(radius) || radius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Circle radius must be finite and positive.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(segments, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(segments, 4096);
        ThrowIfDisposed();

        var previous = center + new Vector2(radius, 0f);
        for (var index = 1; index <= segments; index++)
        {
            var angle = 2f * MathF.PI * index / segments;
            var next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            _backend.DrawLine(previous, next, color);
            previous = next;
        }
    }

    public Texture2D LoadTexture(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ThrowIfDisposed();
        var resource = _backend.LoadTexture(filePath);
        var texture = new Texture2D(this, resource);
        _textures.Add(texture);
        return texture;
    }

    public Texture2D LoadTexture(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ThrowIfDisposed();

        var resource = _backend.LoadTexture(stream);
        var texture = new Texture2D(this, resource);
        _textures.Add(texture);
        return texture;
    }

    public void DrawTexture(Texture2D texture, Vector2 position, Vector2 size)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ValidateRectangle(position, size);
        ThrowIfDisposed();
        _backend.DrawTexture(GetResource(texture), position, size, null, null);
    }

    public void DrawTexture(
        Texture2D texture,
        Vector2 position,
        Vector2 size,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ValidateRectangle(position, size);
        if (sourceX < 0 ||
            sourceY < 0 ||
            sourceWidth <= 0 ||
            sourceHeight <= 0 ||
            sourceX > texture.Width - sourceWidth ||
            sourceY > texture.Height - sourceHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceWidth), "Source rectangle must fit inside the texture.");
        }

        ThrowIfDisposed();
        _backend.DrawTexture(
            GetResource(texture),
            position,
            size,
            new Vector2(sourceX, sourceY),
            new Vector2(sourceWidth, sourceHeight));
    }

    public void DrawWireframe(Mesh mesh, Matrix4x4 worldTransform, Camera3D camera, RenderColor color)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(camera);

        var outputSize = _backend.OutputSize;
        if (outputSize.X <= 0f || outputSize.Y <= 0f)
        {
            return;
        }

        var viewProjection = worldTransform * camera.CreateViewProjection(outputSize.X / outputSize.Y);
        var vertices = mesh.Vertices.Span;
        var indices = mesh.TriangleIndices.Span;
        for (var index = 0; index < indices.Length; index += 3)
        {
            var first = indices[index];
            var second = indices[index + 1];
            var third = indices[index + 2];
            var firstClip = Vector4.Transform(new Vector4(vertices[first], 1f), viewProjection);
            var secondClip = Vector4.Transform(new Vector4(vertices[second], 1f), viewProjection);
            var thirdClip = Vector4.Transform(new Vector4(vertices[third], 1f), viewProjection);

            DrawProjectedSegment(firstClip, secondClip, outputSize, color);
            DrawProjectedSegment(secondClip, thirdClip, outputSize, color);
            DrawProjectedSegment(thirdClip, firstClip, outputSize, color);
        }
    }

    internal void Present()
    {
        ThrowIfDisposed();
        _backend.Present();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        foreach (var texture in _textures.ToArray())
        {
            texture.DisposeFromContext();
        }

        _textures.Clear();
        _backend.Dispose();
    }

    internal void ReleaseTexture(Texture2D texture) => _textures.Remove(texture);

    private IRenderTexture GetResource(Texture2D texture)
    {
        if (!ReferenceEquals(texture.Context, this))
        {
            throw new InvalidOperationException("A texture can only be drawn by the context that created it.");
        }

        return texture.GetResource();
    }

    private void DrawProjectedSegment(Vector4 start, Vector4 end, Vector2 outputSize, RenderColor color)
    {
        if (!ClipSegment(ref start, ref end))
        {
            return;
        }

        var startNdc = new Vector2(start.X, start.Y) / start.W;
        var endNdc = new Vector2(end.X, end.Y) / end.W;
        var startScreen = new Vector2(
            (startNdc.X * 0.5f + 0.5f) * outputSize.X,
            (0.5f - startNdc.Y * 0.5f) * outputSize.Y);
        var endScreen = new Vector2(
            (endNdc.X * 0.5f + 0.5f) * outputSize.X,
            (0.5f - endNdc.Y * 0.5f) * outputSize.Y);
        _backend.DrawLine(startScreen, endScreen, color);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

    private static void ValidateRectangle(Vector2 position, Vector2 size)
    {
        ValidatePoint(position, nameof(position));
        if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0f || size.Y <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Rectangle size must be finite and positive.");
        }
    }

    private static void ValidatePoint(Vector2 point, string parameterName)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Point coordinates must be finite.");
        }
    }

    private static bool ClipSegment(ref Vector4 start, ref Vector4 end)
    {
        const float minimumW = 0.0001f;
        return ClipAgainstPlane(ref start, ref end, vertex => vertex.W - minimumW) &&
               ClipAgainstPlane(ref start, ref end, vertex => vertex.Z) &&
               ClipAgainstPlane(ref start, ref end, vertex => vertex.W - vertex.Z);
    }

    private static bool ClipAgainstPlane(ref Vector4 start, ref Vector4 end, Func<Vector4, float> distance)
    {
        var startDistance = distance(start);
        var endDistance = distance(end);
        if (startDistance < 0f && endDistance < 0f)
        {
            return false;
        }

        if (startDistance < 0f || endDistance < 0f)
        {
            var amount = startDistance / (startDistance - endDistance);
            var intersection = Vector4.Lerp(start, end, amount);
            if (startDistance < 0f)
            {
                start = intersection;
            }
            else
            {
                end = intersection;
            }
        }

        return true;
    }
}
