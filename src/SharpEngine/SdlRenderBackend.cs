using System.Numerics;
using SDL2;

namespace SharpEngine;

internal sealed class SdlRenderBackend(IntPtr renderer) : IRenderBackend
{
    private bool _isDisposed;

    public Vector2 OutputSize
    {
        get
        {
            ThrowIfDisposed();
            if (SDL.SDL_GetRendererOutputSize(renderer, out var width, out var height) < 0)
            {
                throw new InvalidOperationException($"SDL could not get the renderer size: {SDL.SDL_GetError()}");
            }

            return new Vector2(width, height);
        }
    }

    public void Clear(RenderColor color)
    {
        SetDrawColor(color);
        if (SDL.SDL_RenderClear(renderer) < 0)
        {
            throw new InvalidOperationException($"SDL could not clear the frame: {SDL.SDL_GetError()}");
        }
    }

    public void FillRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        SetDrawColor(color);
        var rectangle = ToRectangle(position, size);
        if (SDL.SDL_RenderFillRect(renderer, ref rectangle) < 0)
        {
            throw new InvalidOperationException($"SDL could not fill the rectangle: {SDL.SDL_GetError()}");
        }
    }

    public void DrawRectangle(Vector2 position, Vector2 size, RenderColor color)
    {
        SetDrawColor(color);
        var rectangle = ToRectangle(position, size);
        if (SDL.SDL_RenderDrawRect(renderer, ref rectangle) < 0)
        {
            throw new InvalidOperationException($"SDL could not draw the rectangle: {SDL.SDL_GetError()}");
        }
    }

    public void DrawLine(Vector2 start, Vector2 end, RenderColor color)
    {
        SetDrawColor(color);
        if (SDL.SDL_RenderDrawLine(renderer, (int)start.X, (int)start.Y, (int)end.X, (int)end.Y) < 0)
        {
            throw new InvalidOperationException($"SDL could not draw the line: {SDL.SDL_GetError()}");
        }
    }

    public void Present()
    {
        ThrowIfDisposed();
        SDL.SDL_RenderPresent(renderer);
    }

    public IRenderTexture LoadTexture(Stream stream)
    {
        ThrowIfDisposed();
        var temporaryFile = Path.GetTempFileName();
        try
        {
            using (var file = File.Create(temporaryFile))
            {
                stream.CopyTo(file);
            }

            return LoadTexture(temporaryFile);
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    public IRenderTexture LoadTexture(string filePath)
    {
        ThrowIfDisposed();
        var surface = SDL.SDL_LoadBMP(filePath);
        if (surface == IntPtr.Zero)
        {
            throw new InvalidDataException($"SDL could not decode BMP texture '{filePath}': {SDL.SDL_GetError()}");
        }

        try
        {
            var nativeTexture = SDL.SDL_CreateTextureFromSurface(renderer, surface);
            if (nativeTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL could not create a texture: {SDL.SDL_GetError()}");
            }

            if (SDL.SDL_QueryTexture(nativeTexture, out _, out _, out var width, out var height) < 0)
            {
                SDL.SDL_DestroyTexture(nativeTexture);
                throw new InvalidOperationException($"SDL could not read texture dimensions: {SDL.SDL_GetError()}");
            }

            return new SdlTexture(nativeTexture, width, height);
        }
        finally
        {
            SDL.SDL_FreeSurface(surface);
        }
    }

    public void DrawTexture(
        IRenderTexture texture,
        Vector2 position,
        Vector2 size,
        Vector2? sourcePosition,
        Vector2? sourceSize)
    {
        ThrowIfDisposed();
        if (texture is not SdlTexture sdlTexture)
        {
            throw new InvalidOperationException("The texture was created by a different renderer.");
        }

        var destination = ToRectangle(position, size);
        int result;
        if (sourcePosition is { } sourceStart && sourceSize is { } sourceDimensions)
        {
            var source = ToRectangle(sourceStart, sourceDimensions);
            result = SDL.SDL_RenderCopy(renderer, sdlTexture.NativeHandle, ref source, ref destination);
        }
        else
        {
            result = SDL.SDL_RenderCopy(renderer, sdlTexture.NativeHandle, IntPtr.Zero, ref destination);
        }

        if (result < 0)
        {
            throw new InvalidOperationException($"SDL could not draw texture: {SDL.SDL_GetError()}");
        }
    }

    public void Dispose() => _isDisposed = true;

    private void SetDrawColor(RenderColor color)
    {
        ThrowIfDisposed();
        if (SDL.SDL_SetRenderDrawColor(renderer, color.Red, color.Green, color.Blue, color.Alpha) < 0)
        {
            throw new InvalidOperationException($"SDL could not set the render color: {SDL.SDL_GetError()}");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

    private static SDL.SDL_Rect ToRectangle(Vector2 position, Vector2 size) => new()
    {
        x = (int)position.X,
        y = (int)position.Y,
        w = (int)size.X,
        h = (int)size.Y
    };

    private sealed class SdlTexture(IntPtr nativeHandle, int width, int height) : IRenderTexture
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public IntPtr NativeHandle { get; private set; } = nativeHandle;

        public void Dispose()
        {
            if (NativeHandle == IntPtr.Zero)
            {
                return;
            }

            SDL.SDL_DestroyTexture(NativeHandle);
            NativeHandle = IntPtr.Zero;
        }
    }
}
