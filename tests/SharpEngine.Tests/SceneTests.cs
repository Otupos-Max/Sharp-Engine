using SharpEngine;
using Xunit;

namespace SharpEngine.Tests;

public sealed class SceneTests
{
    [Fact]
    public void StartAndDestroyCallComponentLifecycleOnce()
    {
        using var scene = new Scene();
        var component = new TrackingComponent();
        var gameObject = new GameObject("test");
        gameObject.AddComponent(component);
        scene.Add(gameObject);

        scene.Start();
        scene.Start();
        scene.Update(default, new InputState());
        scene.Dispose();

        Assert.Equal(1, component.StartCount);
        Assert.Equal(1, component.UpdateCount);
        Assert.Equal(1, component.DestroyCount);
    }

    [Fact]
    public void ComponentsAddedAfterSceneStartStartImmediately()
    {
        using var scene = new Scene();
        scene.Start();
        var gameObject = scene.Add(new GameObject("late object"));
        var component = gameObject.AddComponent(new TrackingComponent());

        Assert.Equal(1, component.StartCount);
    }

    [Fact]
    public void DestroyedObjectsAreRemovedAfterUpdate()
    {
        using var scene = new Scene();
        var gameObject = scene.Add(new GameObject("temporary"));
        scene.Start();

        gameObject.Destroy();
        scene.Update(default, new InputState());

        Assert.Empty(scene.GameObjects);
    }

    [Fact]
    public void ObjectCannotBeAttachedToMultipleScenes()
    {
        var gameObject = new GameObject("single owner");
        var firstScene = new Scene();
        var secondScene = new Scene();

        firstScene.Add(gameObject);

        Assert.Throws<InvalidOperationException>(() => secondScene.Add(gameObject));
        firstScene.Dispose();
        secondScene.Dispose();
    }

    private sealed class TrackingComponent : Component
    {
        public int StartCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public override void Start() => StartCount++;

        public override void Update(GameTime time, InputState input) => UpdateCount++;

        public override void Destroy() => DestroyCount++;
    }
}
