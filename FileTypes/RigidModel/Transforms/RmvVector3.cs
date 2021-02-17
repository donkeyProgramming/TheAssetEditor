using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Transforms
{
    [Serializable]
    public struct RmvVector3
    {
        public float X;
        public float Y;
        public float Z;

        public bool IsAllZero()
        {
            if (X == 0 && Y == 0 && Z == 0)
                return true;
            return false;
        }

        public RmvVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
