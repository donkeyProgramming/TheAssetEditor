using System.IO;
using System.Windows.Media.Imaging;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Test.Rendering
{
    // A screenshot keeps the viewport's light. The frame is premultiplied, as the viewport draws it, and additive light (particles,
    // bloom) adds colour and no alpha: saved as it was, Archaon's flames had their colour and alpha 0 over the empty background,
    // and a viewer reading the alpha showed nothing there.
    internal class ScreenshotAlphaTests
    {
        static Color Saved(Color premultiplied)
        {
            using var stream = new MemoryStream();
            RenderEngineComponent.SavePng(stream, [premultiplied], 1, 1);
            stream.Position = 0;
            var frame = new FormatConvertedBitmap(BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0],
                System.Windows.Media.PixelFormats.Bgra32, null, 0);
            var bgra = new byte[4];
            frame.CopyPixels(bgra, 4, 0);
            return new Color(bgra[2], bgra[1], bgra[0], bgra[3]);
        }

        // Over black the saved pixel is what the viewport drew: the light's colour, kept as alpha from its brightness.
        [Test]
        public void LightOverNothing_IsSavedSeen_WithAlphaFromItsBrightness()
        {
            var saved = Saved(new Color(200, 100, 0, 0));

            Assert.That(saved, Is.EqualTo(new Color(255, 128, 0, 200)));
        }

        [TestCase(10, 20, 30, 255)]
        [TestCase(0, 0, 0, 0)]
        public void ASolidOrEmptyPixel_IsSavedAsItIs(int r, int g, int b, int a)
        {
            Assert.That(Saved(new Color(r, g, b, a)), Is.EqualTo(new Color(r, g, b, a)));
        }

        // A half-covered edge was saved premultiplied as if straight: half its colour at half alpha, a dark fringe.
        [Test]
        public void AHalfCoveredEdge_IsSavedAtItsFullColour()
        {
            Assert.That(Saved(new Color(64, 32, 0, 128)), Is.EqualTo(new Color(128, 64, 0, 128)));
        }
    }
}
