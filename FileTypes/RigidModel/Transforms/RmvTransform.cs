using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Transforms
{
    public struct RmvTransform
    {
        public RmvVector3 Pivot;
        RmvMatrix3x4 Matrix0;
        RmvMatrix3x4 Matrix1;
        RmvMatrix3x4 Matrix2;

        public bool IsIdentityPivot()
        {
            if (!Pivot.IsAllZero())
                return false;
            return true;
        }

        public bool IsIdentityMatrices()
        {
            if (!Matrix0.IsIdentity())
                return false;

            if (!Matrix1.IsIdentity())
                return false;

            if (!Matrix2.IsIdentity())
                return false;

            return true;
        }
      
    }
}
