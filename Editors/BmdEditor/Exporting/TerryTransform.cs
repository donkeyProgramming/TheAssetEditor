using Microsoft.Xna.Framework;

namespace Editors.BmdEditor.Exporting
{
    /// <summary>
    /// Converts BMD's row-vector transforms (translation in the matrix's fourth row, same
    /// convention as <see cref="Matrix.Decompose"/>) into the position/XYZ-euler-degrees/scale
    /// triples Terry's ECTransform expects.
    /// </summary>
    public static class TerryTransform
    {
        public readonly record struct Decomposed(Vector3 Position, Vector3 EulerDegrees, Vector3 Scale);

        public static Decomposed Decompose(Matrix m)
        {
            var position = new Vector3(m.M41, m.M42, m.M43);

            var row1 = new Vector3(m.M11, m.M12, m.M13);
            var row2 = new Vector3(m.M21, m.M22, m.M23);
            var row3 = new Vector3(m.M31, m.M32, m.M33);

            var scale = new Vector3(row1.Length(), row2.Length(), row3.Length());

            var a = scale.X > 1e-8f ? row1 / scale.X : row1;
            var d = scale.Y > 1e-8f ? row2 / scale.Y : row2;
            var g = scale.Z > 1e-8f ? row3 / scale.Z : row3;

            var yRad = MathF.Asin(Math.Clamp(g.X, -1f, 1f));
            var xRad = MathF.Atan2(-g.Y, g.Z);
            var zRad = MathF.Atan2(-d.X, a.X);

            var eulerDegrees = new Vector3(MathHelper.ToDegrees(xRad), MathHelper.ToDegrees(yRad), MathHelper.ToDegrees(zRad));
            return new Decomposed(position, eulerDegrees, scale);
        }

        /// <summary>
        /// Inverse of <see cref="Decompose"/>: builds the row-vector transform matrix Terry's
        /// position/XYZ-euler-degrees/scale triple represents. The rotation part is
        /// M = Rz(z) * Ry(y) * Rx(x) (row-vector composition) - verified by substituting its rows
        /// back into Decompose's own asin/atan2 formulas and recovering x, y, z exactly.
        /// </summary>
        public static Matrix Compose(Vector3 position, Vector3 eulerDegrees, Vector3 scale)
        {
            var x = MathHelper.ToRadians(eulerDegrees.X);
            var y = MathHelper.ToRadians(eulerDegrees.Y);
            var z = MathHelper.ToRadians(eulerDegrees.Z);

            var cx = MathF.Cos(x); var sx = MathF.Sin(x);
            var cy = MathF.Cos(y); var sy = MathF.Sin(y);
            var cz = MathF.Cos(z); var sz = MathF.Sin(z);

            var row1 = new Vector3(cy * cz, sz * cx + cz * sy * sx, sz * sx - cz * sy * cx) * scale.X;
            var row2 = new Vector3(-sz * cy, cz * cx - sz * sy * sx, cz * sx + sz * sy * cx) * scale.Y;
            var row3 = new Vector3(sy, -cy * sx, cy * cx) * scale.Z;

            return new Matrix(
                row1.X, row1.Y, row1.Z, 0,
                row2.X, row2.Y, row2.Z, 0,
                row3.X, row3.Y, row3.Z, 0,
                position.X, position.Y, position.Z, 1);
        }

        /// <summary>Standard quaternion-to-XYZ-euler-degrees conversion, used for SpotLightInfo
        /// which stores its orientation as a quaternion rather than a matrix.</summary>
        public static Vector3 QuaternionToEulerDegrees(float qx, float qy, float qz, float qw)
        {
            var ySquared = qy * qy;

            var t0 = 2f * (qw * qx + qy * qz);
            var t1 = 1f - 2f * (qx * qx + ySquared);
            var xRad = MathF.Atan2(t0, t1);

            var t2 = Math.Clamp(2f * (qw * qy - qz * qx), -1f, 1f);
            var yRad = MathF.Asin(t2);

            var t3 = 2f * (qw * qz + qx * qy);
            var t4 = 1f - 2f * (ySquared + qz * qz);
            var zRad = MathF.Atan2(t3, t4);

            return new Vector3(MathHelper.ToDegrees(xRad), MathHelper.ToDegrees(yRad), MathHelper.ToDegrees(zRad));
        }

        /// <summary>Inverse of <see cref="QuaternionToEulerDegrees"/>.</summary>
        public static Quaternion EulerDegreesToQuaternion(Vector3 eulerDegrees)
        {
            var x = MathHelper.ToRadians(eulerDegrees.X);
            var y = MathHelper.ToRadians(eulerDegrees.Y);
            var z = MathHelper.ToRadians(eulerDegrees.Z);

            var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x);
            var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, y);
            var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z);
            return qz * qy * qx;
        }

        /// <summary>
        /// First-order-accurate XYZ delta extraction from a small (single gizmo-drag-tick)
        /// rotation matrix, safe to add directly onto any Euler-degree channel regardless of its
        /// composition order - for infinitesimal rotations, all conventions agree to first order.
        /// </summary>
        public static Vector3 ExtractSmallRotationDeltaDegrees(Matrix delta)
        {
            var xRad = MathF.Atan2(delta.M23, delta.M33);
            var yRad = MathF.Asin(Math.Clamp(-delta.M13, -1f, 1f));
            var zRad = MathF.Atan2(delta.M12, delta.M11);
            return new Vector3(MathHelper.ToDegrees(xRad), MathHelper.ToDegrees(yRad), MathHelper.ToDegrees(zRad));
        }

        /// <summary>
        /// Terry can only hole-punch flat (2D) polylines, but a terrain-hole triangle can sit at
        /// any orientation. This rotates the triangle so its normal aligns with +Y (making it flat
        /// in the local X-Z plane) and returns the placement rotation needed to put it back.
        /// </summary>
        public static (Vector3 Position, Vector3 EulerDegrees, Vector3 LocalVertex2, Vector3 LocalVertex3) FlattenTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var rel2 = v2 - v1;
            var rel3 = v3 - v1;

            var normal = Vector3.Cross(rel2, rel3);
            if (normal.LengthSquared() < 1e-12f)
                return (v1, Vector3.Zero, new Vector3(rel2.X, 0, rel2.Z), new Vector3(rel3.X, 0, rel3.Z));
            normal.Normalize();

            var axis = Vector3.Cross(normal, Vector3.Up);
            if (axis.LengthSquared() < 1e-12f)
                return (v1, Vector3.Zero, new Vector3(rel2.X, 0, rel2.Z), new Vector3(rel3.X, 0, rel3.Z));
            axis.Normalize();

            var angle = MathF.Acos(Math.Clamp(normal.Y, -1f, 1f));

            // Row-vector rotation matrix (v' = v * m) for `angle` around `axis`, built directly
            // via Rodrigues' formula rather than Matrix.CreateFromAxisAngle so the sign convention
            // is known and matches the euler extraction below.
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            var m = new Matrix(
                cos + axis.X * axis.X * (1 - cos), axis.X * axis.Y * (1 - cos) + axis.Z * sin, axis.X * axis.Z * (1 - cos) - axis.Y * sin, 0,
                axis.Y * axis.X * (1 - cos) - axis.Z * sin, cos + axis.Y * axis.Y * (1 - cos), axis.Y * axis.Z * (1 - cos) + axis.X * sin, 0,
                axis.Z * axis.X * (1 - cos) + axis.Y * sin, axis.Z * axis.Y * (1 - cos) - axis.X * sin, cos + axis.Z * axis.Z * (1 - cos), 0,
                0, 0, 0, 1);

            var flatRel2 = Vector3.Transform(rel2, m);
            var flatRel3 = Vector3.Transform(rel3, m);

            var yRad = MathF.Asin(Math.Clamp(m.M13, -1f, 1f));
            var xRad = MathF.Atan2(-m.M23, m.M33);
            var zRad = MathF.Atan2(-m.M12, m.M11);
            var eulerDegrees = new Vector3(MathHelper.ToDegrees(xRad), MathHelper.ToDegrees(yRad), MathHelper.ToDegrees(zRad));

            return (v1, eulerDegrees, flatRel2, flatRel3);
        }
    }
}
