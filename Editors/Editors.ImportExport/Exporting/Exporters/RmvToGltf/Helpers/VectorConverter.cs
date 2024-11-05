using System.Numerics;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    class VecConv
    {
        public static Quaternion GetSys(Microsoft.Xna.Framework.Quaternion q) => new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W);
        public static Vector4 GetSys(Microsoft.Xna.Framework.Vector4 v) => new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
        public static Vector3 GetSys(Microsoft.Xna.Framework.Vector3 v) => new System.Numerics.Vector3(v.X, v.Y, v.Z);

        public static Microsoft.Xna.Framework.Vector4 GetXna(System.Numerics.Vector4 v) => new Microsoft.Xna.Framework.Vector4(v.X, v.Y, v.Z, v.W);
        public static Microsoft.Xna.Framework.Vector3 GetXna(System.Numerics.Vector3 v) => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z);

        public static Matrix4x4 Create4x4SysMatrix(Microsoft.Xna.Framework.Matrix invMatrices) =>
                        new(invMatrices.M11, invMatrices.M12, invMatrices.M13, invMatrices.M14,
                            invMatrices.M21, invMatrices.M22, invMatrices.M23, invMatrices.M24,
                            invMatrices.M31, invMatrices.M32, invMatrices.M33, invMatrices.M34,
                            invMatrices.M41, invMatrices.M42, invMatrices.M43, invMatrices.M44);
        public static Matrix4x4 GetSys(Microsoft.Xna.Framework.Matrix invMatrices) =>
                            new (invMatrices.M11, invMatrices.M12, invMatrices.M13, invMatrices.M14,
                                invMatrices.M21, invMatrices.M22, invMatrices.M23, invMatrices.M24,
                                invMatrices.M31, invMatrices.M32, invMatrices.M33, invMatrices.M34,
                                invMatrices.M41, invMatrices.M42, invMatrices.M43, invMatrices.M44);
    }

}

