using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Utility
{
    public class MathUtil
    {
       public static Vector3 GetCenter(BoundingBox box)
        {
            Vector3 finalPos = Vector3.Zero;
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
                float isqrt = 1.0f / (float)Math.Sqrt(length);
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
            float sa = (float)Math.Sin(angle);
            float ooScale = 0f;
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

    }
}
