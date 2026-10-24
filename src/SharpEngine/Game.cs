namespace SharpEngine;

public abstract class Game
{
    internal bool ExitRequested { get; private set; }

    public virtual void Initialize()
    {
    }

    public abstract void Update(GameTime time, InputState input);

    public abstract void Render(RenderContext context);

    public virtual void Shutdown()
    {
    }

    protected void RequestExit() => ExitRequested = true;
}
