using SDL2;

namespace SharpEngine;

public sealed class RenderContext
{
    private readonly IntPtr _renderer;

    internal RenderContext(IntPtr renderer)
    {
        _renderer = renderer;
    }

    public void Clear(RenderColor color)
    {
        if (SDL.SDL_SetRenderDrawColor(_renderer, color.Red, color.Green, color.Blue, color.Alpha) < 0)
        {
            throw new InvalidOperationException($"SDL could not set the render color: {SDL.SDL_GetError()}");
        }

        if (SDL.SDL_RenderClear(_renderer) < 0)
        {
            throw new InvalidOperationException($"SDL could not clear the frame: {SDL.SDL_GetError()}");
        }
    }

    internal void Present() => SDL.SDL_RenderPresent(_renderer);
}
