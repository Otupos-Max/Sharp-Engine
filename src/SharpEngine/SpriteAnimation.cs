namespace SharpEngine;

public sealed class SpriteAnimation
{
    private readonly SpriteFrame[] _frames;

    public SpriteAnimation(IEnumerable<SpriteFrame> frames, TimeSpan frameDuration, bool isLooping = true)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(frameDuration, TimeSpan.Zero);

        _frames = frames.ToArray();
        if (_frames.Length == 0)
        {
            throw new ArgumentException("An animation must contain at least one frame.", nameof(frames));
        }

        for (var index = 0; index < _frames.Length; index++)
        {
            var frame = _frames[index];
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                throw new ArgumentException($"Animation frame {index} must have a positive size.", nameof(frames));
            }
        }

        FrameDuration = frameDuration;
        IsLooping = isLooping;
    }

    public TimeSpan FrameDuration { get; }

    public bool IsLooping { get; }

    public int FrameCount => _frames.Length;

    public SpriteFrame GetFrame(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _frames.Length);
        return _frames[index];
    }
}
