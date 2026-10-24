using System.Numerics;

namespace SharpEngine;

public sealed class SpriteAnimator : Component
{
    private long _elapsedTicks;
    private Vector2 _size;

    public SpriteAnimator(Texture2D texture, SpriteAnimation animation, Vector2 size)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(animation);
        ValidateSize(size);

        Texture = texture;
        Animation = animation;
        _size = size;
        ValidateFrames(texture, animation);
    }

    public Texture2D Texture { get; }

    public SpriteAnimation Animation { get; private set; }

    public Vector2 Size
    {
        get => _size;
        set
        {
            ValidateSize(value);
            _size = value;
        }
    }

    public int FrameIndex { get; private set; }

    public bool IsComplete { get; private set; }

    public void Play(SpriteAnimation animation, bool restart = true)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ValidateFrames(Texture, animation);

        if (!restart && ReferenceEquals(Animation, animation))
        {
            return;
        }

        Animation = animation;
        FrameIndex = 0;
        _elapsedTicks = 0;
        IsComplete = false;
    }

    public override void Update(GameTime time, InputState input)
    {
        if (time.Elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(time), "Elapsed time cannot be negative.");
        }

        if (IsComplete)
        {
            return;
        }

        var durationTicks = Animation.FrameDuration.Ticks;
        var elapsedTicks = checked(_elapsedTicks + time.Elapsed.Ticks);
        var framesToAdvance = elapsedTicks / durationTicks;
        _elapsedTicks = elapsedTicks % durationTicks;

        if (framesToAdvance == 0)
        {
            return;
        }

        if (Animation.IsLooping)
        {
            FrameIndex = (int)((FrameIndex + framesToAdvance % Animation.FrameCount) % Animation.FrameCount);
            return;
        }

        var framesUntilComplete = Animation.FrameCount - FrameIndex;
        if (framesToAdvance >= framesUntilComplete)
        {
            FrameIndex = Animation.FrameCount - 1;
            _elapsedTicks = 0;
            IsComplete = true;
            return;
        }

        FrameIndex += (int)framesToAdvance;
    }

    public override void Render(RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var frame = Animation.GetFrame(FrameIndex);
        context.DrawTexture(
            Texture,
            new Vector2(Owner.Position.X, Owner.Position.Y),
            Size,
            frame.X,
            frame.Y,
            frame.Width,
            frame.Height);
    }

    private static void ValidateFrames(Texture2D texture, SpriteAnimation animation)
    {
        for (var index = 0; index < animation.FrameCount; index++)
        {
            var frame = animation.GetFrame(index);
            if (frame.X > texture.Width - frame.Width ||
                frame.Y > texture.Height - frame.Height)
            {
                throw new ArgumentException(
                    $"Animation frame {index} extends beyond the texture bounds.",
                    nameof(animation));
            }
        }
    }

    private static void ValidateSize(Vector2 size)
    {
        if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0f || size.Y <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Sprite size must be finite and positive.");
        }
    }
}
