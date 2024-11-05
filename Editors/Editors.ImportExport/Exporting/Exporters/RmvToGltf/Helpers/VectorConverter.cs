using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    class VecConv
    {
        public static System.Numerics.Quaternion GetSys(Microsoft.Xna.Framework.Quaternion q) => new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W);
        public static System.Numerics.Vector4 GetSys(Microsoft.Xna.Framework.Vector4 v) => new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
        public static System.Numerics.Vector3 GetSys(Microsoft.Xna.Framework.Vector3 v) => new System.Numerics.Vector3(v.X, v.Y, v.Z);

        public static System.Numerics.Matrix4x4 Create4x4SysMatrix(Microsoft.Xna.Framework.Matrix invMatrices) =>
            new Matrix4x4(invMatrices.M11, invMatrices.M12, invMatrices.M13, invMatrices.M14,
                            invMatrices.M21, invMatrices.M22, invMatrices.M23, invMatrices.M24,
                            invMatrices.M31, invMatrices.M32, invMatrices.M33, invMatrices.M34,
                            invMatrices.M41, invMatrices.M42, invMatrices.M43, invMatrices.M44);
    public static System.Numerics.Matrix4x4 GetSys(Microsoft.Xna.Framework.Matrix invMatrices) =>
            new Matrix4x4(invMatrices.M11, invMatrices.M12, invMatrices.M13, invMatrices.M14,
                            invMatrices.M21, invMatrices.M22, invMatrices.M23, invMatrices.M24,
                            invMatrices.M31, invMatrices.M32, invMatrices.M33, invMatrices.M34,
                            invMatrices.M41, invMatrices.M42, invMatrices.M43, invMatrices.M44);
    }

}

