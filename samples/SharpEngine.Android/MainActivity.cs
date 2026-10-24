using System.Numerics;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using SharpEngine;

namespace SharpEngine.Android;

[Activity(
    Label = "Sharp Engine",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.NoActionBar",
    ScreenOrientation = ScreenOrientation.Unspecified,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : Activity
{
    private AndroidGameView? _gameView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _gameView = new AndroidGameView(this, new AndroidDemoGame());
        SetContentView(_gameView);
    }

    protected override void OnResume()
    {
        base.OnResume();
        _gameView?.ResumeGame();
    }

    protected override void OnPause()
    {
        _gameView?.PauseGame();
        base.OnPause();
    }

    protected override void OnDestroy()
    {
        _gameView?.ShutdownGame();
        _gameView = null;
        base.OnDestroy();
    }
}

internal sealed class AndroidGameView : View, IDisposable
{
    private readonly Game _game;
    private readonly AndroidCanvasBackend _backend = new();
    private readonly RenderContext _renderContext;
    private readonly InputState _input = new();
    private readonly Handler _handler = new(Looper.MainLooper!);
    private readonly Java.Lang.Runnable _renderRunnable;
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private TimeSpan _previousTime;
    private long _frame;
    private bool _isRunning;
    private bool _isInitialized;
    private bool _isDisposed;

    public AndroidGameView(Activity activity, Game game) : base(activity)
    {
        _game = game;
        _renderContext = new RenderContext(_backend);
        _renderRunnable = new Java.Lang.Runnable(() =>
        {
            if (_isRunning)
            {
                _input.BeginFrame();
                Invalidate();
            }
        });
        Focusable = true;
        FocusableInTouchMode = true;
        KeepScreenOn = true;
        SetBackgroundColor(Color.Rgb(14, 20, 34));
    }

    public void ResumeGame()
    {
        if (_isDisposed || _isRunning)
        {
            return;
        }

        _isRunning = true;
        _clock.Start();
        _previousTime = _clock.Elapsed;
        _handler.Post(_renderRunnable);
    }

    public void PauseGame()
    {
        _isRunning = false;
        _handler.RemoveCallbacks(_renderRunnable);
        _clock.Stop();
        ReleaseTouchKeys();
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        if (!_isRunning || _isDisposed)
        {
            return;
        }

        _backend.BeginFrame(
            canvas,
            Width,
            Height,
            Context?.Assets ?? throw new InvalidOperationException("Android assets are unavailable."));
        if (!_isInitialized)
        {
            _game.Initialize();
            _game.StartScene();
            _isInitialized = true;
        }

        var currentTime = _clock.Elapsed;
        _game.Tick(new GameTime(currentTime - _previousTime, currentTime, _frame), _input);
        if (!_game.ExitRequested)
        {
            _game.Draw(_renderContext);
            _renderContext.Present();
        }

        _previousTime = currentTime;
        _frame++;
        if (_isRunning && !_game.ExitRequested)
        {
            _handler.PostDelayed(_renderRunnable, 16);
        }
        else
        {
            PauseGame();
        }
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null)
        {
            return false;
        }

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
            case MotionEventActions.Move:
                UpdateTouchKeys(e.GetX(), e.GetY());
                return true;
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            case MotionEventActions.Cancel:
                ReleaseTouchKeys();
                return true;
            default:
                return true;
        }
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (TryMapKey(keyCode, out var key))
        {
            _input.SetKey(key, true);
            return true;
        }

        return base.OnKeyDown(keyCode, e);
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
    {
        if (TryMapKey(keyCode, out var key))
        {
            _input.SetKey(key, false);
            return true;
        }

        return base.OnKeyUp(keyCode, e);
    }

    public void ShutdownGame()
    {
        if (_isDisposed)
        {
            return;
        }

        PauseGame();
        _isDisposed = true;
        if (_isInitialized)
        {
            try
            {
                _game.Shutdown();
            }
            finally
            {
                _game.DisposeScene();
            }
        }

        _renderContext.Dispose();
        _clock.Stop();
    }

    private void UpdateTouchKeys(float x, float y)
    {
        _input.SetKey(EngineKey.W, false);
        _input.SetKey(EngineKey.A, false);
        _input.SetKey(EngineKey.S, false);
        _input.SetKey(EngineKey.D, false);
        _input.SetKey(EngineKey.Space, false);

        if (x < Width * 0.55f)
        {
            var centerX = Width * 0.25f;
            var centerY = Height * 0.55f;
            var deltaX = x - centerX;
            var deltaY = y - centerY;
            if (System.Math.Abs(deltaX) > Width * 0.06f)
            {
                _input.SetKey(deltaX < 0 ? EngineKey.A : EngineKey.D, true);
            }

            if (System.Math.Abs(deltaY) > Height * 0.06f)
            {
                _input.SetKey(deltaY < 0 ? EngineKey.W : EngineKey.S, true);
            }
        }
        else
        {
            _input.SetKey(EngineKey.Space, true);
        }
    }

    private void ReleaseTouchKeys()
    {
        foreach (var key in new[]
                 {
                     EngineKey.W, EngineKey.A, EngineKey.S, EngineKey.D, EngineKey.Space
                 })
        {
            _input.SetKey(key, false);
        }
    }

    private static bool TryMapKey(Keycode keyCode, out EngineKey key)
    {
        key = keyCode switch
        {
            Keycode.W => EngineKey.W,
            Keycode.A => EngineKey.A,
            Keycode.S => EngineKey.S,
            Keycode.D => EngineKey.D,
            Keycode.Space => EngineKey.Space,
            Keycode.Enter => EngineKey.Enter,
            Keycode.Escape => EngineKey.Escape,
            Keycode.DpadUp => EngineKey.Up,
            Keycode.DpadDown => EngineKey.Down,
            Keycode.DpadLeft => EngineKey.Left,
            Keycode.DpadRight => EngineKey.Right,
            Keycode.Q => EngineKey.Q,
            Keycode.E => EngineKey.E,
            Keycode.Z => EngineKey.Z,
            Keycode.X => EngineKey.X,
            _ => default
        };

        return keyCode is Keycode.W or Keycode.A or Keycode.S or Keycode.D or Keycode.Space or
            Keycode.Enter or Keycode.Escape or Keycode.DpadUp or Keycode.DpadDown or
            Keycode.DpadLeft or Keycode.DpadRight or Keycode.Q or Keycode.E or Keycode.Z or Keycode.X;
    }
}

internal sealed class AndroidDemoGame : Game
{
    private static readonly Mesh CubeMesh = Mesh.CreateCube(1.8f);
    private Vector2 _playerPosition = new(60, 220);
    private float _elapsed;

    public override void Update(GameTime time, InputState input)
    {
        _elapsed = (float)time.Total.TotalSeconds;
        if (input.IsKeyPressed(EngineKey.Escape))
        {
            RequestExit();
        }

        var movement = Vector2.Zero;
        if (input.IsKeyDown(EngineKey.W) || input.IsKeyDown(EngineKey.Up))
        {
            movement.Y -= 1;
        }

        if (input.IsKeyDown(EngineKey.S) || input.IsKeyDown(EngineKey.Down))
        {
            movement.Y += 1;
        }

        if (input.IsKeyDown(EngineKey.A) || input.IsKeyDown(EngineKey.Left))
        {
            movement.X -= 1;
        }

        if (input.IsKeyDown(EngineKey.D) || input.IsKeyDown(EngineKey.Right))
        {
            movement.X += 1;
        }

        if (input.IsKeyDown(EngineKey.Space))
        {
            movement += new Vector2(1, 0);
        }

        if (movement.LengthSquared() > 0)
        {
            _playerPosition += Vector2.Normalize(movement) * 180f * (float)time.Elapsed.TotalSeconds;
        }
    }

    public override void Render(RenderContext context)
    {
        var size = context.OutputSize;
        context.Clear(new RenderColor(14, 20, 34));
        context.FillRectangle(_playerPosition, new Vector2(48, 48), new RenderColor(70, 210, 150));
        context.DrawRectangle(new Vector2(24, 184), new Vector2(120, 120), new RenderColor(78, 106, 150));
        context.DrawCircle(new Vector2(90, size.Y - 70), 30, new RenderColor(247, 185, 85));

        var camera = new Camera3D(new Vector3(size.X * 0.7f, 0, 6), new Vector3(size.X * 0.7f, 0, 0));
        var rotation = Quaternion.CreateFromYawPitchRoll(
            _elapsed * 0.7f,
            _elapsed * 0.45f,
            0);
        var world = Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(size.X * 0.7f, 0, 0);
        context.DrawWireframe(CubeMesh, world, camera, new RenderColor(90, 190, 255));
    }
}
