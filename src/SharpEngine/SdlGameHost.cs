using System.Diagnostics;
using SDL2;

namespace SharpEngine;

public sealed class SdlGameHost
{
    private readonly string _title;
    private readonly int _width;
    private readonly int _height;

    public SdlGameHost(string title, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _title = title;
        _width = width;
        _height = height;
    }

    public void Run(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            throw new InvalidOperationException($"SDL could not initialize: {SDL.SDL_GetError()}");
        }

        IntPtr window = IntPtr.Zero;
        IntPtr renderer = IntPtr.Zero;
        var gameInitialized = false;

        try
        {
            window = SDL.SDL_CreateWindow(
                _title,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                _width,
                _height,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            if (window == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL could not create a window: {SDL.SDL_GetError()}");
            }

            renderer = SDL.SDL_CreateRenderer(
                window,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (renderer == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL could not create a renderer: {SDL.SDL_GetError()}");
            }

            var input = new InputState();
            var context = new RenderContext(renderer);
            game.Initialize();
            gameInitialized = true;

            var clock = Stopwatch.StartNew();
            var previousTime = clock.Elapsed;
            long frame = 0;
            var isRunning = true;

            while (isRunning && !game.ExitRequested)
            {
                while (SDL.SDL_PollEvent(out var @event) == 1)
                {
                    switch (@event.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            isRunning = false;
                            break;
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            SetKeyState(input, @event.key.keysym.sym, true);
                            break;
                        case SDL.SDL_EventType.SDL_KEYUP:
                            SetKeyState(input, @event.key.keysym.sym, false);
                            break;
                    }
                }

                if (!isRunning)
                {
                    break;
                }

                var currentTime = clock.Elapsed;
                game.Update(new GameTime(currentTime - previousTime, currentTime, frame), input);
                if (game.ExitRequested)
                {
                    break;
                }

                game.Render(context);
                context.Present();

                previousTime = currentTime;
                frame++;
            }
        }
        finally
        {
            try
            {
                if (gameInitialized)
                {
                    game.Shutdown();
                }
            }
            finally
            {
                if (renderer != IntPtr.Zero)
                {
                    SDL.SDL_DestroyRenderer(renderer);
                }

                if (window != IntPtr.Zero)
                {
                    SDL.SDL_DestroyWindow(window);
                }

                SDL.SDL_Quit();
            }
        }
    }

    private static void SetKeyState(InputState input, SDL.SDL_Keycode key, bool isDown)
    {
        var engineKey = key switch
        {
            SDL.SDL_Keycode.SDLK_ESCAPE => EngineKey.Escape,
            SDL.SDL_Keycode.SDLK_w => EngineKey.W,
            SDL.SDL_Keycode.SDLK_a => EngineKey.A,
            SDL.SDL_Keycode.SDLK_s => EngineKey.S,
            SDL.SDL_Keycode.SDLK_d => EngineKey.D,
            SDL.SDL_Keycode.SDLK_SPACE => EngineKey.Space,
            SDL.SDL_Keycode.SDLK_RETURN => EngineKey.Enter,
            _ => (EngineKey?)null
        };

        if (engineKey.HasValue)
        {
            input.SetKey(engineKey.Value, isDown);
        }
    }
}
