using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Shared.CoreTest.Misc
{
    public class MathUtilEulerTests
    {
        static readonly object[] AxisCases =
        [
            new object[] { new Vector3(30, 0, 0), Vector3.UnitX },
            new object[] { new Vector3(0, 30, 0), Vector3.UnitY },
            new object[] { new Vector3(0, 0, 30), Vector3.UnitZ },
        ];

        // Regression for #106: the field labelled Z must rotate about Z, not X.
        [TestCaseSource(nameof(AxisCases))]
        public void EulerDegreesToQuaternion_EachComponentRotatesAboutItsOwnAxis(Vector3 eulerDegrees, Vector3 axis)
        {
            var expected = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(30));
            var actual = MathUtil.EulerDegreesToQuaternion(eulerDegrees);

            Assert.That(Math.Abs(Quaternion.Dot(expected, actual)), Is.EqualTo(1).Within(1e-5));
        }

        [TestCase(30, 0, 0)]
        [TestCase(0, 30, 0)]
        [TestCase(0, 0, 30)]
        [TestCase(10, 20, 30)]
        public void QuaternionToEulerDegree_RoundTripsKeepTheSameComponents(float x, float y, float z)
        {
            var input = new Vector3(x, y, z);
            var output = MathUtil.QuaternionToEulerDegree(MathUtil.EulerDegreesToQuaternion(input));

            Assert.That(output.X, Is.EqualTo(input.X).Within(1e-3));
            Assert.That(output.Y, Is.EqualTo(input.Y).Within(1e-3));
            Assert.That(output.Z, Is.EqualTo(input.Z).Within(1e-3));
        }
    }
}
