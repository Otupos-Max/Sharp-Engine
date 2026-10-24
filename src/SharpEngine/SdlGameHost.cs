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
        RenderContext? context = null;
        var gameInitialized = false;

        try
        {
            window = SDL.SDL_CreateWindow(
                _title,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                _width,
                _height,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
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
                renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
                if (renderer == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"SDL could not create a renderer: {SDL.SDL_GetError()}");
                }
            }

            var input = new InputState();
            context = new RenderContext(renderer);
            game.Initialize();
            gameInitialized = true;
            game.StartScene();

            var clock = Stopwatch.StartNew();
            var previousTime = clock.Elapsed;
            long frame = 0;
            var isRunning = true;

            while (isRunning && !game.ExitRequested)
            {
                input.BeginFrame();
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
                game.Tick(new GameTime(currentTime - previousTime, currentTime, frame), input);
                if (game.ExitRequested)
                {
                    break;
                }

                game.Draw(context);
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
                    try
                    {
                        game.Shutdown();
                    }
                    finally
                    {
                        game.DisposeScene();
                    }
                }
            }
            finally
            {
                if (renderer != IntPtr.Zero)
                {
                    context?.Dispose();
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
            SDL.SDL_Keycode.SDLK_UP => EngineKey.Up,
            SDL.SDL_Keycode.SDLK_DOWN => EngineKey.Down,
            SDL.SDL_Keycode.SDLK_LEFT => EngineKey.Left,
            SDL.SDL_Keycode.SDLK_RIGHT => EngineKey.Right,
            SDL.SDL_Keycode.SDLK_q => EngineKey.Q,
            SDL.SDL_Keycode.SDLK_e => EngineKey.E,
            SDL.SDL_Keycode.SDLK_z => EngineKey.Z,
            SDL.SDL_Keycode.SDLK_x => EngineKey.X,
            _ => (EngineKey?)null
        };

        if (engineKey.HasValue)
        {
            input.SetKey(engineKey.Value, isDown);
        }
    }
}
