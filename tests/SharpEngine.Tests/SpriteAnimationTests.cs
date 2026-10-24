using System.Numerics;
using SharpEngine;
using Xunit;

namespace SharpEngine.Tests;

public sealed class SpriteAnimationTests
{
    [Fact]
    public void LoopingAnimationAdvancesAcrossMultipleFramesAndWraps()
    {
        using var context = new RenderContext(new TestRenderBackend(32, 16));
        var texture = context.LoadTexture(new MemoryStream());
        var animation = new SpriteAnimation(
            [new SpriteFrame(0, 0, 16, 16), new SpriteFrame(16, 0, 16, 16)],
            TimeSpan.FromMilliseconds(100));
        var animator = new SpriteAnimator(texture, animation, new Vector2(32, 32));

        animator.Update(new GameTime(TimeSpan.FromMilliseconds(250), default, 0), new InputState());

        Assert.Equal(0, animator.FrameIndex);
        Assert.False(animator.IsComplete);

        animator.Update(new GameTime(TimeSpan.FromMilliseconds(100), default, 1), new InputState());

        Assert.Equal(1, animator.FrameIndex);
    }

    [Fact]
    public void NonLoopingAnimationStopsOnLastFrameAndCanBeRestarted()
    {
        using var context = new RenderContext(new TestRenderBackend(32, 16));
        var texture = context.LoadTexture(new MemoryStream());
        var animation = new SpriteAnimation(
            [new SpriteFrame(0, 0, 16, 16), new SpriteFrame(16, 0, 16, 16)],
            TimeSpan.FromMilliseconds(100),
            isLooping: false);
        var animator = new SpriteAnimator(texture, animation, new Vector2(32, 32));

        animator.Update(new GameTime(TimeSpan.FromMilliseconds(300), default, 0), new InputState());

        Assert.Equal(1, animator.FrameIndex);
        Assert.True(animator.IsComplete);

        animator.Play(animation);

        Assert.Equal(0, animator.FrameIndex);
        Assert.False(animator.IsComplete);
    }

    [Fact]
    public void PlayCanKeepTheCurrentAnimationProgress()
    {
        using var context = new RenderContext(new TestRenderBackend(32, 16));
        var texture = context.LoadTexture(new MemoryStream());
        var animation = new SpriteAnimation(
            [new SpriteFrame(0, 0, 16, 16), new SpriteFrame(16, 0, 16, 16)],
            TimeSpan.FromMilliseconds(100));
        var animator = new SpriteAnimator(texture, animation, new Vector2(32, 32));
        animator.Update(new GameTime(TimeSpan.FromMilliseconds(100), default, 0), new InputState());

        animator.Play(animation, restart: false);

        Assert.Equal(1, animator.FrameIndex);
    }

    [Fact]
    public void RenderUsesTheCurrentFrameAndOwnerPosition()
    {
        var backend = new TestRenderBackend(32, 16);
        using var context = new RenderContext(backend);
        var texture = context.LoadTexture(new MemoryStream());
        var animation = new SpriteAnimation(
            [new SpriteFrame(0, 0, 16, 16), new SpriteFrame(16, 0, 16, 16)],
            TimeSpan.FromMilliseconds(100));
        var gameObject = new GameObject
        {
            Position = new Vector3(12, 24, 0)
        };
        var animator = gameObject.AddComponent(new SpriteAnimator(texture, animation, new Vector2(32, 32)));
        animator.Update(new GameTime(TimeSpan.FromMilliseconds(100), default, 0), new InputState());

        animator.Render(context);

        Assert.Equal(new Vector2(12, 24), backend.LastDrawPosition);
        Assert.Equal(new Vector2(32, 32), backend.LastDrawSize);
        Assert.Equal(new Vector2(16, 0), backend.LastSourcePosition);
        Assert.Equal(new Vector2(16, 16), backend.LastSourceSize);
    }

    [Fact]
    public void AnimationRejectsEmptyFramesAndFramesOutsideTexture()
    {
        Assert.Throws<ArgumentException>(() =>
            new SpriteAnimation([], TimeSpan.FromMilliseconds(100)));
        Assert.Throws<ArgumentException>(() =>
            new SpriteAnimation([default], TimeSpan.FromMilliseconds(100)));

        using var context = new RenderContext(new TestRenderBackend(16, 16));
        var texture = context.LoadTexture(new MemoryStream());
        var invalidAnimation = new SpriteAnimation(
            [new SpriteFrame(8, 8, 16, 16)],
            TimeSpan.FromMilliseconds(100));

        Assert.Throws<ArgumentException>(() =>
            new SpriteAnimator(texture, invalidAnimation, new Vector2(16, 16)));
    }

    private sealed class TestRenderBackend(int width, int height) : IRenderBackend
    {
        public Vector2 OutputSize => new(width, height);

        public Vector2? LastDrawPosition { get; private set; }

        public Vector2? LastDrawSize { get; private set; }

        public Vector2? LastSourcePosition { get; private set; }

        public Vector2? LastSourceSize { get; private set; }

        public void Clear(RenderColor color) { }

        public void FillRectangle(Vector2 position, Vector2 size, RenderColor color) { }

        public void DrawRectangle(Vector2 position, Vector2 size, RenderColor color) { }

        public void DrawLine(Vector2 start, Vector2 end, RenderColor color) { }

        public void Present() { }

        public IRenderTexture LoadTexture(string filePath) => new TestRenderTexture(width, height);

        public IRenderTexture LoadTexture(Stream stream) => new TestRenderTexture(width, height);

        public void DrawTexture(
            IRenderTexture texture,
            Vector2 position,
            Vector2 size,
            Vector2? sourcePosition,
            Vector2? sourceSize)
        {
            LastDrawPosition = position;
            LastDrawSize = size;
            LastSourcePosition = sourcePosition;
            LastSourceSize = sourceSize;
        }

        public void Dispose() { }
    }

    private sealed class TestRenderTexture(int width, int height) : IRenderTexture
    {
        public int Width => width;

        public int Height => height;

        public void Dispose() { }
    }
}
