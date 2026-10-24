namespace SharpEngine;

public sealed class Texture2D : IDisposable
{
    private IntPtr _nativeTexture;

    internal Texture2D(RenderContext context, IntPtr nativeTexture, int width, int height)
    {
        Context = context;
        _nativeTexture = nativeTexture;
        Width = width;
        Height = height;
    }

    internal RenderContext Context { get; }

    public int Width { get; }

    public int Height { get; }

    public bool IsDisposed => _nativeTexture == IntPtr.Zero;

    public void Dispose()
    {
        if (_nativeTexture == IntPtr.Zero)
        {
            return;
        }

        var nativeTexture = _nativeTexture;
        _nativeTexture = IntPtr.Zero;
        Context.ReleaseTexture(this);
        SDL2.SDL.SDL_DestroyTexture(nativeTexture);
        GC.SuppressFinalize(this);
    }

    internal IntPtr GetNativeHandle()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return _nativeTexture;
    }

    internal void DisposeFromContext()
    {
        if (_nativeTexture == IntPtr.Zero)
        {
            return;
        }

        SDL2.SDL.SDL_DestroyTexture(_nativeTexture);
        _nativeTexture = IntPtr.Zero;
    }
}
