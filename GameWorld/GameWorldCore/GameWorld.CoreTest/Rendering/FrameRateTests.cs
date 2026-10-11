using GameWorld.Core.WpfWindow;

namespace GameWorld.Core.Test.Rendering
{
    // WPF raises CompositionTarget.Rendering once per display refresh, not at a fixed 60 Hz, and
    // D3D11Host only draws on the ticks IsFrameDue lets through. A cap that does not divide the
    // refresh period drops whole ticks: a 16 ms cap on a 75 Hz display drew every other one.
    internal class FrameRateTests
    {
        [TestCase(60)]
        [TestCase(75)]
        [TestCase(100)]
        [TestCase(144)]
        public void DefaultTarget_DrawsEveryDisplayRefresh(int refreshHz)
        {
            var period = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / refreshHz);
            var lastRendered = TimeSpan.Zero;
            var framesDrawn = 0;

            for (var tick = 1; tick <= refreshHz; tick++)
            {
                var renderingTime = period * tick;
                if (D3D11Host.IsFrameDue(lastRendered, renderingTime, D3D11Host.DefaultTargetElapsedTime))
                {
                    framesDrawn++;
                    lastRendered = renderingTime;
                }
            }

            Assert.That(framesDrawn, Is.EqualTo(refreshHz), $"frames drawn in one second at {refreshHz} Hz");
        }
    }
}
