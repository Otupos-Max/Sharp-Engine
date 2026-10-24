namespace SharpEngine;

public sealed class InputState
{
    private readonly HashSet<EngineKey> _keysDown = [];
    private readonly HashSet<EngineKey> _keysPressed = [];
    private readonly HashSet<EngineKey> _keysReleased = [];

    public bool IsKeyDown(EngineKey key) => _keysDown.Contains(key);

    public bool IsKeyPressed(EngineKey key) => _keysPressed.Contains(key);

    public bool IsKeyReleased(EngineKey key) => _keysReleased.Contains(key);

    internal void BeginFrame()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
    }

    internal void SetKey(EngineKey key, bool isDown)
    {
        if (isDown && _keysDown.Add(key))
        {
            _keysPressed.Add(key);
        }
        else if (!isDown && _keysDown.Remove(key))
        {
            _keysReleased.Add(key);
        }
    }
}
