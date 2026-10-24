using System.Numerics;
using SharpEngine;

var game = new FirstGame();
var host = new SdlGameHost("Sharp Engine - First Demo", 1280, 720);
host.Run(game);

internal sealed class FirstGame : Game
{
    private Vector2 _playerPosition = new(72, 300);

    public override void Initialize()
    {
        var cube = new GameObject("Rotating cube")
        {
            Position = new Vector3(1.7f, 0, 0)
        };
        cube.AddComponent(new RotatingCube());
        Scene.Add(cube);
    }

    public override void Update(GameTime time, InputState input)
    {
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

        if (movement.LengthSquared() > 0)
        {
            _playerPosition += Vector2.Normalize(movement) * 220f * (float)time.Elapsed.TotalSeconds;
        }
    }

    public override void Render(RenderContext context)
    {
        context.Clear(new RenderColor(14, 20, 34));
        context.FillRectangle(_playerPosition, new Vector2(48, 48), new RenderColor(70, 210, 150));
        context.DrawRectangle(new Vector2(36, 264), new Vector2(120, 120), new RenderColor(78, 106, 150));
        context.DrawCircle(new Vector2(96, 440), 34, new RenderColor(247, 185, 85));
    }
}

internal sealed class RotatingCube : Component
{
    private static readonly Mesh CubeMesh = Mesh.CreateCube(1.8f);
    private static readonly Camera3D Camera = new(new Vector3(1.7f, 0, 6), new Vector3(1.7f, 0, 0));

    public override void Update(GameTime time, InputState input)
    {
        var seconds = (float)time.Total.TotalSeconds;
        Owner.Rotation = Quaternion.CreateFromYawPitchRoll(seconds * 0.7f, seconds * 0.45f, 0);
    }

    public override void Render(RenderContext context)
    {
        context.DrawWireframe(CubeMesh, Owner.TransformMatrix, Camera, new RenderColor(90, 190, 255));
    }
}
