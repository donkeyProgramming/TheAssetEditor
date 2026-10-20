using System.Globalization;
using Shared.GameFormats.Vfx;

namespace GameWorld.Core.Test.Vfx
{
    public class VfxCurveTests
    {
        // One output between a min and a max channel, each running from (0, 0) to (1, 1); the
        // segment eases by the first key's out-interpolation.
        static string Segment(string interpolation, string random = "0") =>
            $"x_min,8405056,0;0.0,0.0,{interpolation},{interpolation},-0.05,0.0,0.05,0.0,1;1.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,1" +
            $"|x_max,16728128,0;0.0,0.0,{interpolation},{interpolation},-0.05,0.0,0.05,0.0,1;1.0,1.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,{random};0,1";

        [TestCase("linear", 0.25f)]
        [TestCase("step", 0f)]
        [TestCase("step_next", 1f)]
        [TestCase("s", 0.15625f)]
        [TestCase("s_inv", 0.32635f)]
        [TestCase("sine", 0.14645f)]
        [TestCase("sine_recip", 0.33333f)]
        [TestCase("log1", 0.35737f)]
        [TestCase("log3", 0.5843f)]
        [TestCase("exp1", 0.1653f)]
        [TestCase("exp3", 0.05853f)]
        [TestCase("bezier_c", 0.23813f)]
        [TestCase("bezier_q", 0.23813f)]
        public void Interpolation_EndsAtItsKeys_AndShapesTheSegment(string interpolation, float atQuarter)
        {
            var curve = VfxCurve.Parse(Segment(interpolation));
            Assert.That(curve, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(curve.Evaluate(0, 0, 0), Is.EqualTo(0).Within(1e-4));
                Assert.That(curve.Evaluate(0, 0.25f, 0), Is.EqualTo(atQuarter).Within(1e-3));
                Assert.That(curve.Evaluate(0, 1, 0), Is.EqualTo(1).Within(1e-4));
            });
        }

        [Test]
        public void KeysOutOfTimeOrder_AreSortedFirst()
        {
            // 105k vanilla channels list their keys like this: the key at 1.0 before the key at 0.0.
            var curve = VfxCurve.Parse(
                "x_min,0,0;1.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1;0.0,0.0,linear,linear,-0.05,0.0,0.05,0.0,1" +
                "|x_max,0,0;1.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1;0.0,0.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,0;0,1");
            Assert.That(curve.Evaluate(0, 0.5f, 0), Is.EqualTo(5).Within(1e-4));
        }

        [TestCase("1", 12.5f, Description = "random 1: a point between min and max")]
        [TestCase("0", 10f, Description = "random 0: the min")]
        public void MinMax_FollowTheRandomFlag(string random, float expected)
        {
            var curve = VfxCurve.Parse(
                $"x_min,0,0;0.0,10.0,linear,linear,-0.05,0.0,0.05,0.0,1|x_max,0,0;0.0,20.0,linear,linear,-0.05,0.0,0.05,0.0,1:x,{random};0,1");
            Assert.That(curve.Evaluate(0, 0.5f, 0.25f), Is.EqualTo(expected).Within(1e-4));
        }

        [Test]
        public void Outputs_ReadTheirOwnChannels_AndAMissingOneFallsBack()
        {
            var curve = VfxCurve.Parse(
                "x_min,0,0;0.0,1.0,linear,linear,0,0,0,0,1|x_max,0,0;0.0,1.0,linear,linear,0,0,0,0,1" +
                "|y_min,0,0;0.0,2.0,linear,linear,0,0,0,0,1|y_max,0,0;0.0,2.0,linear,linear,0,0,0,0,1:x,0;0,1|y,0;2,3");
            Assert.That((curve.Evaluate(0, 0, 0), curve.Evaluate(1, 0, 0), curve.Evaluate(2, 0, 0, fallback: 7)), Is.EqualTo((1f, 2f, 7f)));
        }

        [Test]
        public void Constant_HoldsOneValuePerOutput()
        {
            var curve = VfxCurve.Constant(3, 4);
            Assert.That((curve.Evaluate(0, 0.7f, 0.9f), curve.Evaluate(1, 0.2f, 0.1f)), Is.EqualTo((3f, 4f)));
        }

        [TestCase("", Description = "empty")]
        [TestCase("garbage", Description = "no mapping")]
        [TestCase("x_min,0,0;0.0,1.0|x_max,0,0;0.0,1.0:x,0;0,1", Description = "keys with 2 fields, not 9")]
        [TestCase("x_min,0,0;0.0,1.0,linear,linear,0,0,0,0:x,0;0,0", Description = "a key with 8 fields, not 9")]
        [TestCase("x_min,0,0;0.0,1.0,linear,linear,0,0,0,0,1:x,0;0,3", Description = "a mapping past the last channel")]
        [TestCase("x_min,0,0;zero,1.0,linear,linear,0,0,0,0,1:x,0;0,0", Description = "a number that is not one")]
        // float.Parse reads these two as infinity and NaN; a modded rate of either hung the spawn loop.
        [TestCase("x_min,0,0;0.0,1e39,linear,linear,0,0,0,0,1:x,0;0,0", Description = "a value past float's range")]
        [TestCase("x_min,0,0;0.0,NaN,linear,linear,0,0,0,0,1:x,0;0,0", Description = "NaN")]
        public void BrokenText_DoesNotParse(string text) => Assert.That(VfxCurve.Parse(text), Is.Null);
    }
}
