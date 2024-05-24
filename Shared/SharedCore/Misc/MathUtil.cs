using Microsoft.Xna.Framework;

namespace Shared.Core.Misc
{
    public class MathUtil
    {
        public static T EnsureRange<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(max) > 0)
                return max;
            else if (value.CompareTo(min) < 0)
                return min;
            return value;
        }

        public static bool CompareEqualFloats(float lhs, float rhs = 0f, float tolerance = 1E-6f)
        {
            return Math.Abs(lhs - rhs) < tolerance;
        }

        public static Vector3 GetCenter(BoundingBox box)
        {
            var finalPos = Vector3.Zero;
            var corners = box.GetCorners();
            foreach (var corner in corners)
                finalPos += corner;

            return finalPos / corners.Length;
        }

        /// <summary>
        /// Converts from degrees to radians. A present from the PlazSoft team.
        /// </summary>
        public static float ToRadians(float angle)
        {
            return MathHelper.ToRadians(angle);
        }
        /// <summary>
        /// Converts from radians to degrees.
        /// </summary>
        public static float ToDegrees(float angle)
        {
            return MathHelper.ToDegrees(angle);
        }

        /// <summary>
        /// Normalizes the vector and returns its length. A present from the PlazSoft team.
        /// If the vector is zero, the vector itself is returned.
        /// </summary>
        /// <param name="v">Vector to normalize</param>
        /// <param name="length">Length of vector before normalization</param>
        /// <returns>The normalized vector or (0,0,0)</returns>
        public static Vector3 NormalizeSafelyAndLength(Vector3 v, out float length)
        {
            if (v.X == 0 && v.Y == 0 && v.Z == 0)
            {
                length = 0;
                return v;
            }
            length = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
            if (length != 0)
            {
                var isqrt = 1.0f / (float)Math.Sqrt(length);
                length *= isqrt;
                v.X *= isqrt;
                v.Y *= isqrt;
                v.Z *= isqrt;
            }
            return v;
        }

        /// <summary>
        /// Converts a quaternion into a human readable format. A present from the PlazSoft team.
        /// This will not correctly convert quaternions with length
        /// above 1.
        /// 
        /// Reversable with FromAxisAngleDegrees(Vector3)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Vector3 ToAxisAngleDegrees(Quaternion q)
        {
            Vector3 axis;
            float angle;
            ToAxisAngle(q, out axis, out angle);
            return axis * ToDegrees(angle);
        }

        /// <summary>
        /// Converts from human readable axis angle degree representation
        /// to a quaternion. A present from the PlazSoft team.
        /// 
        /// Reversable with ToAxisAngleDegrees(this Quaternion)
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static Quaternion FromAxisAngleDegrees(Vector3 deg)
        {
            float length;
            deg = NormalizeSafelyAndLength(deg, out length);
            return Quaternion.CreateFromAxisAngle(deg, ToRadians(length));
        }

        /// <summary>
        /// Converts a quaternion to axis/angle representation. A present from the PlazSoft team.
        /// 
        /// Reversable with Quaternion.CreateFromAxisAngle(Vector3, float)
        /// </summary>
        /// <param name="q"></param>
        /// <param name="axis">The axis that is rotated around, or (0,0,0)</param>
        /// <param name="angle">Angle around axis, in radians</param>
        public static void ToAxisAngle(Quaternion q, out Vector3 axis, out float angle)
        {
            //From
            //http://social.msdn.microsoft.com/Forums/en-US/c482c19a-c566-4a64-aa9c-7a79ba7564d6/the-reverse-of-quaternioncreatefromaxisangle?forum=xnaframework
            //Modified to return 0,0,0 when it would have returned NaN
            //due to divide by zero.
            angle = (float)Math.Acos(q.W);
            var sa = (float)Math.Sin(angle);
            var ooScale = 0f;
            if (sa != 0)
                ooScale = 1.0f / sa;
            angle *= 2.0f;

            axis = new Vector3(q.X, q.Y, q.Z) * ooScale;
        }

        public static Matrix CreateRotation(float x_degrees, float y_degrees, float z_degrees)
        {
            var x = MathHelper.ToRadians(x_degrees);
            var y = MathHelper.ToRadians(y_degrees);
            var z = MathHelper.ToRadians(z_degrees);
            return Matrix.CreateRotationX(x) * Matrix.CreateRotationY(y) * Matrix.CreateRotationZ(z);
        }

        public static Matrix CreateRotation(Vector3 angles_degrees)
        {
            var x = MathHelper.ToRadians(angles_degrees.X);
            var y = MathHelper.ToRadians(angles_degrees.Y);
            var z = MathHelper.ToRadians(angles_degrees.Z);
            return Matrix.CreateRotationX(x) * Matrix.CreateRotationY(y) * Matrix.CreateRotationZ(z);
        }

        public static Matrix CreateRotation(Vector3[] vectors, Vector3[] basis = null)
        {
            if (vectors.Length != 3)
            {
                throw new ArgumentException("Number of vectors should be equal to 3");
            }
            if (basis != null)
            {
                if (vectors.Length != basis.Length || basis.Length != 3)
                {
                    throw new ArgumentException("Number of vectors should match and be equal to 3");
                }
            }

            if (basis == null)
            {
                basis = new Vector3[]
                {
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1),
                };
            }
            var rotationM = new Matrix(
                Vector3.Dot(basis[0], vectors[0]), Vector3.Dot(basis[1], vectors[0]), Vector3.Dot(basis[2], vectors[0]), 0,
                Vector3.Dot(basis[0], vectors[1]), Vector3.Dot(basis[1], vectors[1]), Vector3.Dot(basis[2], vectors[1]), 0,
                Vector3.Dot(basis[0], vectors[2]), Vector3.Dot(basis[1], vectors[2]), Vector3.Dot(basis[2], vectors[2]), 0,
                0, 0, 0, 1
            );
            return rotationM;
        }

        public static Vector3 SanitiseScaleVector(Vector3 v)
        {
            if (v.X <= 0 || float.IsNaN(v.X))
                v.X = 0.00001f;

            if (v.Y <= 0 || float.IsNaN(v.Y))
                v.Y = 0.00001f;

            if (v.Z <= 0 || float.IsNaN(v.Z))
                v.Z = 0.00001f;

            return v;
        }

        public static void Barycentric(Vector3 a, Vector3 b, Vector3 c, Vector3 p, out float u, out float v, out float w)
        {
            var v0 = b - a;
            var v1 = c - a;
            var v2 = p - a;
            var d00 = Vector3.Dot(v0, v0);
            var d01 = Vector3.Dot(v0, v1);
            var d11 = Vector3.Dot(v1, v1);
            var d20 = Vector3.Dot(v2, v0);
            var d21 = Vector3.Dot(v2, v1);
            var denom = d00 * d11 - d01 * d01;
            v = (d11 * d20 - d01 * d21) / denom;
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }

        public static Vector3 ClosestPtPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            //http://www.r-5.org/files/books/computers/algo-list/realtime-3d/Christer_Ericson-Real-Time_Collision_Detection-EN.pdf
            var ab = b - a;
            var ac = c - a;
            var bc = c - b;
            // Compute parametric position s for projection P’ of P on AB,
            // P’ = A + s*AB, s = snom/(snom+sdenom)

            var snom = Vector3.Dot(p - a, ab);
            var sdenom = Vector3.Dot(p - b, a - b);

            // Compute parametric position t for projection P’ of P on AC,
            // P’ = A + t*AC, s = tnom/(tnom+tdenom)

            var tnom = Vector3.Dot(p - a, ac);
            var tdenom = Vector3.Dot(p - c, a - c);
            if (snom <= 0.0f && tnom <= 0.0f) return a; // Vertex region early out
                                                        // Compute parametric position u for projection P’ of P on BC,
                                                        // P’ = B + u*BC, u = unom/(unom+udenom)
            var unom = Vector3.Dot(p - b, bc);
            var udenom = Vector3.Dot(p - c, b - c);
            if (sdenom <= 0.0f && unom <= 0.0f) return b; // Vertex region early out
            if (tdenom <= 0.0f && udenom <= 0.0f) return c; // Vertex region early out
                                                            // P is outside (or on) AB if the triple scalar product [N PA PB] <= 0
            var n = Vector3.Cross(b - a, c - a);

            var vc = Vector3.Dot(n, Vector3.Cross(a - p, b - p));
            // If P outside AB and within feature region of AB,
            // return projection of P onto AB
            if (vc <= 0.0f && snom >= 0.0f && sdenom >= 0.0f)
                return a + snom / (snom + sdenom) * ab;
            // P is outside (or on) BC if the triple scalar product [N PB PC] <= 0
            var va = Vector3.Dot(n, Vector3.Cross(b - p, c - p));
            // If P outside BC and within feature region of BC,
            // return projection of P onto BC
            if (va <= 0.0f && unom >= 0.0f && udenom >= 0.0f)
                return b + unom / (unom + udenom) * bc;
            // P is outside (or on) CA if the triple scalar product [N PC PA] <= 0
            var vb = Vector3.Dot(n, Vector3.Cross(c - p, a - p));

            // If P outside CA and within feature region of CA,
            // return projection of P onto CA
            if (vb <= 0.0f && tnom >= 0.0f && tdenom >= 0.0f)
                return a + tnom / (tnom + tdenom) * ac;

            // P must project inside face region. Compute Q using barycentric coordinates
            var u = va / (va + vb + vc);
            var v = vb / (va + vb + vc);
            var w = 1.0f - u - v; // = vc / (va + vb + vc)

            return u * a + v * b + w * c;
        }

        public static Vector3 QuaternionToEuler(Quaternion q1)
        {
            double heading = 0;
            double attitude = 0;
            double bank = 0;

            double test = q1.X * q1.Y + q1.Z * q1.W;
            if (test > 0.499)
            { // singularity at north pole
                heading = 2 * Math.Atan2(q1.X, q1.W);
                attitude = Math.PI / 2;
                bank = 0;
                return new Vector3((float)heading, (float)attitude, (float)bank);
            }
            if (test < -0.499)
            { // singularity at south pole
                heading = -2 * Math.Atan2(q1.X, q1.W);
                attitude = -Math.PI / 2;
                bank = 0;
                return new Vector3((float)heading, (float)attitude, (float)bank);
            }
            double sqx = q1.X * q1.X;
            double sqy = q1.Y * q1.Y;
            double sqz = q1.Z * q1.Z;
            heading = Math.Atan2(2 * q1.Y * q1.W - 2 * q1.X * q1.Z, 1 - 2 * sqy - 2 * sqz);
            attitude = Math.Asin(2 * test);
            bank = Math.Atan2(2 * q1.X * q1.W - 2 * q1.Y * q1.Z, 1 - 2 * sqx - 2 * sqz);
            return new Vector3((float)heading, (float)attitude, (float)bank);
        }

        public static Vector3 QuaternionToEulerDegree(Quaternion quaternions)
        {
            var euler = QuaternionToEuler(quaternions);
            var degrees = new Vector3(MathHelper.ToDegrees(euler.X), MathHelper.ToDegrees(euler.Y), MathHelper.ToDegrees(euler.Z));
            return degrees;
        }

        public static Quaternion EulerToQuaternions(double heading, double attitude, double bank)
        {
            // Assuming the angles are in radians.
            var c1 = Math.Cos(heading);
            var s1 = Math.Sin(heading);
            var c2 = Math.Cos(attitude);
            var s2 = Math.Sin(attitude);
            var c3 = Math.Cos(bank);
            var s3 = Math.Sin(bank);
            var w = Math.Sqrt(1.0 + c1 * c2 + c1 * c3 - s1 * s2 * s3 + c2 * c3) / 2.0;
            var w4 = 4.0 * w;
            var x = (c2 * s3 + c1 * s3 + s1 * s2 * c3) / w4;
            var y = (s1 * c2 + s1 * c3 + c1 * s2 * s3) / w4;
            var z = (-s1 * s3 + c1 * s2 * c3 + s2) / w4;

            return new Quaternion((float)x, (float)y, (float)z, (float)w);
        }

        public static Quaternion EulerDegreesToQuaternion(Vector3 eulerDegrees)
        {
            var x = MathHelper.ToRadians(eulerDegrees.X);
            var y = MathHelper.ToRadians(eulerDegrees.Y);
            var z = MathHelper.ToRadians(eulerDegrees.Z);
            return EulerToQuaternions(x, y, z);
        }


    }
}
