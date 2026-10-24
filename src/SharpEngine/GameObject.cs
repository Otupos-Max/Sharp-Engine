using System.Numerics;

namespace SharpEngine;

public sealed class GameObject
{
    private readonly List<Component> _components = [];
    private Scene? _scene;
    private bool _started;

    public GameObject(string name = "GameObject")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public Vector3 Position { get; set; }

    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public Vector3 Scale { get; set; } = Vector3.One;

    public Matrix4x4 TransformMatrix =>
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateFromQuaternion(Rotation) *
        Matrix4x4.CreateTranslation(Position);

    public IReadOnlyList<Component> Components => _components;

    public T AddComponent<T>(T component) where T : Component
    {
        ArgumentNullException.ThrowIfNull(component);
        if (_scene?.IsDisposed == true)
        {
            throw new InvalidOperationException("Cannot add a component to an object in a disposed scene.");
        }

        component.Attach(this);
        _components.Add(component);
        if (_started)
        {
            component.StartOnce();
        }

        return component;
    }

    public T? GetComponent<T>() where T : Component =>
        _components.OfType<T>().FirstOrDefault();

    public void Destroy()
    {
        IsActive = false;
    }

    internal void Attach(Scene scene)
    {
        if (_scene is not null)
        {
            throw new InvalidOperationException("A game object can only be added to one scene.");
        }

        _scene = scene;
    }

    internal void StartOnce()
    {
        if (_started || !IsActive)
        {
            return;
        }

        _started = true;
        foreach (var component in _components.ToArray())
        {
            component.StartOnce();
        }
    }

    internal void Update(GameTime time, InputState input)
    {
        foreach (var component in _components.ToArray())
        {
            if (!IsActive)
            {
                break;
            }

            if (component.Enabled)
            {
                component.Update(time, input);
            }
        }
    }

    internal void Render(RenderContext context)
    {
        foreach (var component in _components.ToArray())
        {
            if (!IsActive)
            {
                break;
            }

            if (component.Enabled)
            {
                component.Render(context);
            }
        }
    }

    internal void DestroyComponents()
    {
        foreach (var component in _components.ToArray())
        {
            if (component.HasStarted)
            {
                component.Destroy();
            }
        }

        _components.Clear();
    }
}
