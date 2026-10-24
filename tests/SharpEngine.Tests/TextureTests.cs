using System.Numerics;
using SharpEngine;
using Xunit;

namespace SharpEngine.Tests;

public sealed class TextureTests
{
    [SdlFact]
    public void BitmapTextureLoadsDrawsAndIsDisposedWithHost()
    {
        var bitmapPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");
        var game = new TextureTestGame(bitmapPath);

        try
        {
            WriteOnePixelBitmap(bitmapPath);

            try
            {
                new SdlGameHost("Texture test", 16, 16).Run(game);
            }
            catch (InvalidOperationException exception) when (
                exception.Message.Contains("No available video device", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "SDL was configured with a video driver, but initialization failed.",
                    exception);
            }

            Assert.NotNull(game.Texture);
            Assert.Equal(1, game.Texture.Width);
            Assert.Equal(1, game.Texture.Height);
            Assert.True(game.Texture.IsDisposed);
        }
        finally
        {
            File.Delete(bitmapPath);
        }
    }

    public sealed class SdlFactAttribute : FactAttribute
    {
        public SdlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER")) &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")) &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            {
                Skip = "Set SDL_VIDEODRIVER or run with a desktop display to enable the SDL integration test.";
            }
        }
    }

    private static void WriteOnePixelBitmap(string path)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0x4D42);
        writer.Write(58);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(1);
        writer.Write(1);
        writer.Write((ushort)1);
        writer.Write((ushort)24);
        writer.Write(0);
        writer.Write(4);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(new byte[] { 0, 0, 255, 0 });
    }

    private sealed class TextureTestGame(string bitmapPath) : Game
    {
        public Texture2D? Texture { get; private set; }

        public override void Update(GameTime time, InputState input)
        {
            if (time.Frame > 0)
            {
                RequestExit();
            }
        }

        public override void Render(RenderContext context)
        {
            var texture = Texture ??= context.LoadTexture(bitmapPath);
            context.DrawTexture(texture, Vector2.Zero, new Vector2(16, 16));
            context.DrawTexture(texture, Vector2.Zero, new Vector2(8, 8), 0, 0, 1, 1);
        }
    }
}
