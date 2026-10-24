using System.Numerics;
using Android.Content.Res;
using Android.Graphics;
using SharpEngine;
using IRenderTexture = SharpEngine.IRenderTexture;

namespace SharpEngine.Android;

internal sealed class AndroidCanvasBackend : IRenderBackend
{
    private Canvas? _canvas;
    private AssetManager? _assets;
    private Vector2 _outputSize;
    private bool _isDisposed;

    public Vector2 OutputSize => _outputSize;

    public void BeginFrame(Canvas canvas, int width, int height, AssetManager assets)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _canvas = canvas;
        _assets = assets;
        _outputSize = new Vector2(width, height);
    }

    public void Clear(RenderColor color)
    {
        RequireCanvas().DrawColor(ToColor(color));
    }

    public void FillRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        using var paint = CreatePaint(color, Paint.Style.Fill!);
        RequireCanvas().DrawRect(position.X, position.Y, position.X + size.X, position.Y + size.Y, paint);
    }

    public void DrawRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        using var paint = CreatePaint(color, Paint.Style.Stroke!);
        RequireCanvas().DrawRect(position.X, position.Y, position.X + size.X, position.Y + size.Y, paint);
    }

    public void DrawLine(Vector2 start, Vector2 end, RenderColor color)
    {
        using var paint = CreatePaint(color, Paint.Style.Stroke!);
        paint.StrokeWidth = 1.5f;
        RequireCanvas().DrawLine(start.X, start.Y, end.X, end.Y, paint);
    }

    public IRenderTexture LoadTexture(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        var bitmap = BitmapFactory.DecodeStream(stream);
        if (bitmap is null)
        {
            throw new InvalidDataException("Android could not decode the image texture.");
        }

        return new AndroidBitmapTexture(bitmap);
    }

    public IRenderTexture LoadTexture(string filePath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        var assets = _assets ??
                     throw new InvalidOperationException("Android assets are only available while the game view is active.");
        using var stream = assets.Open(filePath);
        return LoadTexture(stream);
    }

    public void DrawTexture(
        IRenderTexture texture,
        Vector2 position,
        Vector2 size,
        Vector2? sourcePosition,
        Vector2? sourceSize)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (texture is not AndroidBitmapTexture bitmapTexture)
        {
            throw new InvalidOperationException("The texture was created by a different renderer.");
        }

        Rect? source = null;
        if (sourcePosition is { } sourceStart && sourceSize is { } sourceDimensions)
        {
            source = new Rect(
                (int)sourceStart.X,
                (int)sourceStart.Y,
                (int)(sourceStart.X + sourceDimensions.X),
                (int)(sourceStart.Y + sourceDimensions.Y));
        }

        var destination = new RectF(position.X, position.Y, position.X + size.X, position.Y + size.Y);
        using var paint = CreatePaint(new RenderColor(255, 255, 255), Paint.Style.Fill!);
        RequireCanvas().DrawBitmap(bitmapTexture.Bitmap, source, destination, paint);
    }

    public void Present()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _canvas = null;
        _assets = null;
    }

    private Canvas RequireCanvas()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _canvas ?? throw new InvalidOperationException("Android rendering is only available during a frame.");
    }

    private static Paint CreatePaint(RenderColor color, Paint.Style style)
    {
        var paint = new Paint
        {
            Color = ToColor(color),
            AntiAlias = true,
            FilterBitmap = true
        };
        paint.SetStyle(style);
        return paint;
    }

    private static Color ToColor(RenderColor color) => new(color.Red, color.Green, color.Blue, color.Alpha);

    private sealed class AndroidBitmapTexture(Bitmap bitmap) : IRenderTexture
    {
        public Bitmap Bitmap { get; } = bitmap;

        public int Width => Bitmap.Width;

        public int Height => Bitmap.Height;

        public void Dispose() => Bitmap.Dispose();
    }
}
