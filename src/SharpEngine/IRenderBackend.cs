using System.Numerics;

namespace SharpEngine;

internal interface IRenderBackend : IDisposable
{
    Vector2 OutputSize { get; }

    void Clear(RenderColor color);

    void FillRectangle(Vector2 position, Vector2 size, RenderColor color);

    void DrawRectangle(Vector2 position, Vector2 size, RenderColor color);

    void DrawLine(Vector2 start, Vector2 end, RenderColor color);

    void Present();

    IRenderTexture LoadTexture(string filePath);

    IRenderTexture LoadTexture(Stream stream);

    void DrawTexture(
        IRenderTexture texture,
        Vector2 position,
        Vector2 size,
        Vector2? sourcePosition,
        Vector2? sourceSize);
}

internal interface IRenderTexture : IDisposable
{
    int Width { get; }

    int Height { get; }
}
