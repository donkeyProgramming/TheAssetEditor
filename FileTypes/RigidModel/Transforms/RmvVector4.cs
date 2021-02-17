using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Transforms
{
    [Serializable]
    public struct RmvVector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public RmvVector4(float x, float y, float z, float w = 1)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
}
