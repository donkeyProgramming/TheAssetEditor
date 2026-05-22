using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using Microsoft.Xna.Framework;

namespace Test.GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    [TestFixture]
    public class CompareHelperTests
    {
        [Test]
        public void CompareFloat_WithinTolerance_ReturnsTrue()
        {
            var a = 1.00f;
            var b = 1.00019f;

            var ok = CompareHelper.Compare(a, b, "float", out var result);
            Assert.That(ok, Is.True);
            Assert.That(result.Result, Is.True);
        }

        [Test]
        public void CompareFloat_OutsideTolerance_ReturnsFalse()
        {
            var a = 1.00f;
            var b = 1.03f; // outside 0.02 tolerance

            var ok = CompareHelper.Compare(a, b, "float", out var result);
            Assert.That(ok, Is.False);
            Assert.That(result.Result, Is.False);
        }

        [Test]
        public void CompareVector2_WithinTolerance_ReturnsTrue()
        {
            var a = new Vector2(1.0f, 2.0f);
            var b = new Vector2(1.0001f, 2.00015f);

            var ok = CompareHelper.Compare(a, b, "vec2", out var result);
            Assert.That(ok, Is.True);
            Assert.That(result.Result, Is.True);
        }

        [Test]
        public void CompareVector2_OutsideTolerance_ReturnsFalse()
        {
            var a = new Vector2(1.0f, 2.0f);
            var b = new Vector2(1.03f, 2.0f);

            var ok = CompareHelper.Compare(a, b, "vec2", out var result);
            Assert.That(ok, Is.False);
            Assert.That(result.Result, Is.False);
        }

        [Test]
        public void CompareVector3_WithinTolerance_ReturnsTrue()
        {
            var a = new Vector3(1.0f, 2.0f, 3.0f);
            var b = new Vector3(1.0001f, 2.0001f, 3.00019f);

            var ok = CompareHelper.Compare(a, b, "vec3", out var result);
            Assert.That(ok, Is.True);
            Assert.That(result.Result, Is.True);
        }

        [Test]
        public void CompareVector3_OutsideTolerance_ReturnsFalse()
        {
            var a = new Vector3(1.0f, 2.0f, 3.0f);
            var b = new Vector3(1.03f, 2.0f, 3.0f);

            var ok = CompareHelper.Compare(a, b, "vec3", out var result);
            Assert.That(ok, Is.False);
            Assert.That(result.Result, Is.False);
        }

        [Test]
        public void CompareVector4_WithinTolerance_ReturnsTrue()
        {
            var a = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
            var b = new Vector4(1.0001f, 2.0001f, 3.0001f, 4.00019f);

            var ok = CompareHelper.Compare(a, b, "vec4", out var result);
            Assert.That(ok, Is.True);
            Assert.That(result.Result, Is.True);
        }

        [Test]
        public void CompareVector4_OutsideTolerance_ReturnsFalse()
        {
            var a = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
            var b = new Vector4(1.03f, 2.0f, 3.0f, 4.0f);

            var ok = CompareHelper.Compare(a, b, "vec4", out var result);
            Assert.That(ok, Is.False);
            Assert.That(result.Result, Is.False);
        }
    }
}
