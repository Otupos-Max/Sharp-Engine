namespace SharpEngine;

public abstract class Component
{
    public GameObject Owner { get; private set; } = null!;

    public bool Enabled { get; set; } = true;

    internal bool HasStarted { get; private set; }

    public virtual void Start()
    {
    }

    public virtual void Update(GameTime time, InputState input)
    {
    }

    public virtual void Render(RenderContext context)
    {
    }

    public virtual void Destroy()
    {
    }

    internal void Attach(GameObject owner)
    {
        if (Owner is not null)
        {
            throw new InvalidOperationException("A component can only belong to one game object.");
        }

        Owner = owner;
    }

    internal void StartOnce()
    {
        if (HasStarted)
        {
            return;
        }

        HasStarted = true;
        Start();
    }
}
