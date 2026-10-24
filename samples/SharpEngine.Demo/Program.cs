using SharpEngine;

var game = new FirstGame();
var host = new SdlGameHost("Sharp Engine - First Demo", 1280, 720);
host.Run(game);

internal sealed class FirstGame : Game
{
    public override void Update(GameTime time, InputState input)
    {
        if (input.IsKeyDown(EngineKey.Escape))
        {
            RequestExit();
        }
    }

    public override void Render(RenderContext context)
    {
        context.Clear(new RenderColor(22, 30, 46));
    }
}
