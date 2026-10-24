namespace SharpEngine;

public sealed class Texture2D : IDisposable
{
    private IRenderTexture? _resource;
    private readonly int _width;
    private readonly int _height;

    internal Texture2D(RenderContext context, IRenderTexture resource)
    {
        Context = context;
        _resource = resource;
        _width = resource.Width;
        _height = resource.Height;
    }

    internal RenderContext Context { get; }

    public int Width => _width;

    public int Height => _height;

    public bool IsDisposed => _resource is null;

    public void Dispose()
    {
        if (_resource is null)
        {
            return;
        }

        var resource = _resource;
        _resource = null;
        Context.ReleaseTexture(this);
        resource.Dispose();
        GC.SuppressFinalize(this);
    }

    internal IRenderTexture GetResource()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return _resource!;
    }

    internal void DisposeFromContext()
    {
        _resource?.Dispose();
        _resource = null;
    }
}
