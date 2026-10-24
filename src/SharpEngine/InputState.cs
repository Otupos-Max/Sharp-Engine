namespace SharpEngine;

public sealed class InputState
{
    private readonly HashSet<EngineKey> _keysDown = [];

    public bool IsKeyDown(EngineKey key) => _keysDown.Contains(key);

    internal void SetKey(EngineKey key, bool isDown)
    {
        if (isDown)
        {
            _keysDown.Add(key);
        }
        else
        {
            _keysDown.Remove(key);
        }
    }
}
