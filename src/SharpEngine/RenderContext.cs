using System.Numerics;
using SDL2;

namespace SharpEngine;

public sealed class RenderContext
{
    private readonly IntPtr _renderer;
    private readonly HashSet<Texture2D> _textures = [];
    private bool _isDisposed;

    internal RenderContext(IntPtr renderer)
    {
        _renderer = renderer;
    }

    public void Clear(RenderColor color)
    {
        SetDrawColor(color);
        if (SDL.SDL_RenderClear(_renderer) < 0)
        {
            throw new InvalidOperationException($"SDL could not clear the frame: {SDL.SDL_GetError()}");
        }
    }

    public void FillRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        ValidateRectangle(position, size);
        SetDrawColor(color);
        var rectangle = new SDL.SDL_Rect
        {
            x = (int)position.X,
            y = (int)position.Y,
            w = (int)size.X,
            h = (int)size.Y
        };

        if (SDL.SDL_RenderFillRect(_renderer, ref rectangle) < 0)
        {
            throw new InvalidOperationException($"SDL could not fill the rectangle: {SDL.SDL_GetError()}");
        }
    }

    public void DrawRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        ValidateRectangle(position, size);
        SetDrawColor(color);
        var rectangle = new SDL.SDL_Rect
        {
            x = (int)position.X,
            y = (int)position.Y,
            w = (int)size.X,
            h = (int)size.Y
        };

        if (SDL.SDL_RenderDrawRect(_renderer, ref rectangle) < 0)
        {
            throw new InvalidOperationException($"SDL could not draw the rectangle: {SDL.SDL_GetError()}");
        }
    }

    public void DrawLine(Vector2 start, Vector2 end, RenderColor color)
    {
        ValidatePoint(start, nameof(start));
        ValidatePoint(end, nameof(end));
        SetDrawColor(color);
        DrawLineUnchecked(start, end);
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
        SetDrawColor(color);

        var previous = center + new Vector2(radius, 0f);
        for (var index = 1; index <= segments; index++)
        {
            var angle = 2f * MathF.PI * index / segments;
            var next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            DrawLineUnchecked(previous, next);
            previous = next;
        }
    }

    public Texture2D LoadTexture(string filePath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var surface = SDL.SDL_LoadBMP(filePath);
        if (surface == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"SDL could not load bitmap '{filePath}': {SDL.SDL_GetError()}");
        }

        try
        {
            var nativeTexture = SDL.SDL_CreateTextureFromSurface(_renderer, surface);
            if (nativeTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"SDL could not create texture from '{filePath}': {SDL.SDL_GetError()}");
            }

            if (SDL.SDL_QueryTexture(nativeTexture, out _, out _, out var width, out var height) < 0)
            {
                SDL.SDL_DestroyTexture(nativeTexture);
                throw new InvalidOperationException(
                    $"SDL could not read texture dimensions for '{filePath}': {SDL.SDL_GetError()}");
            }

            var texture = new Texture2D(this, nativeTexture, width, height);
            _textures.Add(texture);
            return texture;
        }
        finally
        {
            SDL.SDL_FreeSurface(surface);
        }
    }

    public void DrawTexture(Texture2D texture, Vector2 position, Vector2 size)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ValidateRectangle(position, size);
        var nativeTexture = GetNativeTexture(texture);
        var destination = new SDL.SDL_Rect
        {
            x = (int)position.X,
            y = (int)position.Y,
            w = (int)size.X,
            h = (int)size.Y
        };

        if (SDL.SDL_RenderCopy(_renderer, nativeTexture, IntPtr.Zero, ref destination) < 0)
        {
            throw new InvalidOperationException($"SDL could not draw texture: {SDL.SDL_GetError()}");
        }
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

        var nativeTexture = GetNativeTexture(texture);
        var source = new SDL.SDL_Rect
        {
            x = sourceX,
            y = sourceY,
            w = sourceWidth,
            h = sourceHeight
        };
        var destination = new SDL.SDL_Rect
        {
            x = (int)position.X,
            y = (int)position.Y,
            w = (int)size.X,
            h = (int)size.Y
        };

        if (SDL.SDL_RenderCopy(_renderer, nativeTexture, ref source, ref destination) < 0)
        {
            throw new InvalidOperationException($"SDL could not draw texture region: {SDL.SDL_GetError()}");
        }
    }

    public void DrawWireframe(Mesh mesh, Matrix4x4 worldTransform, Camera3D camera, RenderColor color)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(camera);

        if (SDL.SDL_GetRendererOutputSize(_renderer, out var width, out var height) < 0)
        {
            throw new InvalidOperationException($"SDL could not get the renderer size: {SDL.SDL_GetError()}");
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var viewProjection = worldTransform * camera.CreateViewProjection((float)width / height);
        var vertices = mesh.Vertices.Span;
        var indices = mesh.TriangleIndices.Span;
        SetDrawColor(color);
        for (var index = 0; index < indices.Length; index += 3)
        {
            var first = indices[index];
            var second = indices[index + 1];
            var third = indices[index + 2];
            var firstClip = Vector4.Transform(new Vector4(vertices[first], 1f), viewProjection);
            var secondClip = Vector4.Transform(new Vector4(vertices[second], 1f), viewProjection);
            var thirdClip = Vector4.Transform(new Vector4(vertices[third], 1f), viewProjection);

            DrawProjectedSegment(firstClip, secondClip, width, height);
            DrawProjectedSegment(secondClip, thirdClip, width, height);
            DrawProjectedSegment(thirdClip, firstClip, width, height);
        }
    }

    internal void Present()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        SDL.SDL_RenderPresent(_renderer);
    }

    internal void Dispose()
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
    }

    private static void ValidateRectangle(Vector2 position, Vector2 size)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Rectangle position must be finite.");
        }

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

    private void SetDrawColor(RenderColor color)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (SDL.SDL_SetRenderDrawColor(_renderer, color.Red, color.Green, color.Blue, color.Alpha) < 0)
        {
            throw new InvalidOperationException($"SDL could not set the render color: {SDL.SDL_GetError()}");
        }
    }

    private IntPtr GetNativeTexture(Texture2D texture)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (!ReferenceEquals(texture.Context, this))
        {
            throw new InvalidOperationException("A texture can only be drawn by the context that created it.");
        }

        return texture.GetNativeHandle();
    }

    internal void ReleaseTexture(Texture2D texture) => _textures.Remove(texture);

    private void DrawLineUnchecked(Vector2 start, Vector2 end)
    {
        if (SDL.SDL_RenderDrawLine(_renderer, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y) < 0)
        {
            throw new InvalidOperationException($"SDL could not draw the line: {SDL.SDL_GetError()}");
        }
    }

    private void DrawProjectedSegment(Vector4 start, Vector4 end, int width, int height)
    {
        if (!ClipSegment(ref start, ref end))
        {
            return;
        }

        var startNdc = new Vector2(start.X, start.Y) / start.W;
        var endNdc = new Vector2(end.X, end.Y) / end.W;
        var startScreen = new Vector2(
            (startNdc.X * 0.5f + 0.5f) * width,
            (0.5f - startNdc.Y * 0.5f) * height);
        var endScreen = new Vector2(
            (endNdc.X * 0.5f + 0.5f) * width,
            (0.5f - endNdc.Y * 0.5f) * height);
        DrawLineUnchecked(startScreen, endScreen);
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
