using SharpEngine;
using Xunit;

namespace SharpEngine.Tests;

public sealed class InputStateTests
{
    [Fact]
    public void PressAndReleaseEdgesAreReportedForOneFrame()
    {
        var input = new InputState();

        input.SetKey(EngineKey.Space, true);
        Assert.True(input.IsKeyDown(EngineKey.Space));
        Assert.True(input.IsKeyPressed(EngineKey.Space));
        Assert.False(input.IsKeyReleased(EngineKey.Space));

        input.BeginFrame();
        Assert.True(input.IsKeyDown(EngineKey.Space));
        Assert.False(input.IsKeyPressed(EngineKey.Space));

        input.SetKey(EngineKey.Space, false);
        Assert.False(input.IsKeyDown(EngineKey.Space));
        Assert.True(input.IsKeyReleased(EngineKey.Space));

        input.BeginFrame();
        Assert.False(input.IsKeyReleased(EngineKey.Space));
    }
}
