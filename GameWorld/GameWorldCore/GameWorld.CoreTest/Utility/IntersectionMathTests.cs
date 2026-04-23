using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Test.TestUtility;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Test.Utility
{
    [TestFixture]
    public class IntersectionMathTests
    {
        [Test]
        public void IntersectVertex_ScreenSpace_FindsClosestVertex()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var viewProjection = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                                  Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

            var clipPos = Vector4.Transform(new Vector4(0, 0, 0, 1), viewProjection);
            var screenX = (clipPos.X / clipPos.W + 1) * 0.5f * 800;
            var screenY = (1 - clipPos.Y / clipPos.W) * 0.5f * 600;

            var result = IntersectionMath.IntersectVertex(
                new Vector2(screenX, screenY), mesh, Matrix.Identity,
                viewProjection, 800, 600, out var selectedVertex);

            Assert.That(result, Is.Not.Null);
            Assert.That(selectedVertex, Is.EqualTo(0));
        }

        [Test]
        public void IntersectVertex_ScreenSpace_ReturnsNull_WhenFarFromVertices()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var viewProjection = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                                  Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

            var result = IntersectionMath.IntersectVertex(
                new Vector2(0, 0), mesh, Matrix.Identity,
                viewProjection, 800, 600, out var selectedVertex);

            Assert.That(result, Is.Null);
            Assert.That(selectedVertex, Is.EqualTo(-1));
        }

        [Test]
        public void IntersectVertex_ScreenSpace_SelectsCloserVertex()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            var viewProjection = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up) *
                                  Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);

            var clipPos = Vector4.Transform(new Vector4(1, 0, 0, 1), viewProjection);
            var screenX = (clipPos.X / clipPos.W + 1) * 0.5f * 800;
            var screenY = (1 - clipPos.Y / clipPos.W) * 0.5f * 600;

            var result = IntersectionMath.IntersectVertex(
                new Vector2(screenX, screenY), mesh, Matrix.Identity,
                viewProjection, 800, 600, out var selectedVertex);

            Assert.That(result, Is.Not.Null);
            Assert.That(selectedVertex, Is.EqualTo(1));
        }

        [Test]
        public void TransformBoundingBox_AppliesTranslation()
        {
            var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            var translation = Matrix.CreateTranslation(10, 0, 0);

            var result = IntersectionMath.TransformBoundingBox(box, translation);

            Assert.That(result.Min.X, Is.EqualTo(9).Within(0.001f));
            Assert.That(result.Max.X, Is.EqualTo(11).Within(0.001f));
        }

        [Test]
        public void TransformBoundingBox_AppliesScale()
        {
            var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            var scale = Matrix.CreateScale(2);

            var result = IntersectionMath.TransformBoundingBox(box, scale);

            Assert.That(result.Min.X, Is.EqualTo(-2).Within(0.001f));
            Assert.That(result.Max.X, Is.EqualTo(2).Within(0.001f));
        }

        [Test]
        public void IntersectObject_ReturnNull_WhenRayMissesBoundingBox()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            typeof(MeshObject).GetProperty("BoundingBox")?.SetValue(mesh,
                new BoundingBox(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(1.1f, 1.1f, 0.1f)));

            var ray = new Ray(new Vector3(100, 100, 5), new Vector3(0, 0, -1));
            var result = IntersectionMath.IntersectObject(ray, mesh, Matrix.Identity);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void IntersectFace_ReturnNull_WhenRayMissesBoundingBox()
        {
            var mesh = MeshTestHelper.CreateTriangleMesh();
            typeof(MeshObject).GetProperty("BoundingBox")?.SetValue(mesh,
                new BoundingBox(new Vector3(-0.1f, -0.1f, -0.1f), new Vector3(1.1f, 1.1f, 0.1f)));

            var ray = new Ray(new Vector3(100, 100, 5), new Vector3(0, 0, -1));
            var result = IntersectionMath.IntersectFace(ray, mesh, Matrix.Identity, out var face);

            Assert.That(result, Is.Null);
            Assert.That(face, Is.Null);
        }
    }
}
