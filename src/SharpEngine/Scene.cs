namespace SharpEngine;

public sealed class Scene : IDisposable
{
    private readonly List<GameObject> _gameObjects = [];
    private bool _started;

    public IReadOnlyList<GameObject> GameObjects => _gameObjects;

    internal bool IsDisposed { get; private set; }

    public GameObject Add(GameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        gameObject.Attach(this);
        _gameObjects.Add(gameObject);
        if (_started)
        {
            gameObject.StartOnce();
        }

        return gameObject;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_started)
        {
            return;
        }

        _started = true;
        foreach (var gameObject in _gameObjects.ToArray())
        {
            gameObject.StartOnce();
        }
    }

    public void Update(GameTime time, InputState input)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!_started)
        {
            Start();
        }

        foreach (var gameObject in _gameObjects.ToArray())
        {
            if (gameObject.IsActive)
            {
                gameObject.StartOnce();
                gameObject.Update(time, input);
            }
        }

        RemoveDestroyedObjects();
    }

    public void Render(RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        foreach (var gameObject in _gameObjects.ToArray())
        {
            if (gameObject.IsActive)
            {
                gameObject.Render(context);
            }
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        foreach (var gameObject in _gameObjects.ToArray())
        {
            gameObject.DestroyComponents();
        }

        _gameObjects.Clear();
    }

    private void RemoveDestroyedObjects()
    {
        for (var index = _gameObjects.Count - 1; index >= 0; index--)
        {
            var gameObject = _gameObjects[index];
            if (!gameObject.IsActive)
            {
                gameObject.DestroyComponents();
                _gameObjects.RemoveAt(index);
            }
        }
    }
}
