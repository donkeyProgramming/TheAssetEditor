using System.Numerics;

namespace Editors.ImportExport.Common
{
    class VecConv
    {
        public static Vector4 NormalizeTangentVector4(Vector4 tangent)
        {
            // normalize only the xyz components of the tangent, the w component is the handedness (1 or -1) in sharpGLTF
            var tempTangent = Vector3.Normalize(new Vector3(tangent.X, tangent.Y, tangent.Z));
            return new Vector4(tempTangent.X, tempTangent.Y, tempTangent.Z, tangent.W);
        }

        public static Quaternion GetSys(Microsoft.Xna.Framework.Quaternion q) => new Quaternion(q.X, q.Y, q.Z, q.W);
        public static Vector4 GetSys(Microsoft.Xna.Framework.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
        public static Vector3 GetSys(Microsoft.Xna.Framework.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static Microsoft.Xna.Framework.Vector4 GetXna(Vector4 v) => new Microsoft.Xna.Framework.Vector4(v.X, v.Y, v.Z, v.W);
        public static Microsoft.Xna.Framework.Vector3 GetXna(Vector3 v) => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
        public static Microsoft.Xna.Framework.Vector2 GetXna(Vector2 v) => new Microsoft.Xna.Framework.Vector2(v.X, v.Y);
        public static Microsoft.Xna.Framework.Vector4 GetXnaVector4(Vector3 v) => new Microsoft.Xna.Framework.Vector4(v.X, v.Y, v.Z, 0);
        public static Microsoft.Xna.Framework.Vector3 GetXnaVector3(Vector4 v) => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);
        public static Matrix4x4 GetSys(Microsoft.Xna.Framework.Matrix invMatrices) =>
                            new(invMatrices.M11, invMatrices.M12, invMatrices.M13, invMatrices.M14,
                                invMatrices.M21, invMatrices.M22, invMatrices.M23, invMatrices.M24,
                                invMatrices.M31, invMatrices.M32, invMatrices.M33, invMatrices.M34,
                                invMatrices.M41, invMatrices.M42, invMatrices.M43, invMatrices.M44);
    }

}

