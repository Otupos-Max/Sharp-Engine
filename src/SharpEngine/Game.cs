namespace SharpEngine;

public abstract class Game
{
    internal bool ExitRequested { get; private set; }

    public Scene Scene { get; } = new();

    public virtual void Initialize()
    {
    }

    public virtual void Update(GameTime time, InputState input)
    {
    }

    public virtual void Render(RenderContext context)
    {
    }

    public virtual void Shutdown()
    {
    }

    internal void StartScene() => Scene.Start();

    internal void Tick(GameTime time, InputState input)
    {
        Update(time, input);
        Scene.Update(time, input);
    }

    internal void Draw(RenderContext context)
    {
        Render(context);
        Scene.Render(context);
    }

    internal void DisposeScene() => Scene.Dispose();

    protected void RequestExit() => ExitRequested = true;
}
