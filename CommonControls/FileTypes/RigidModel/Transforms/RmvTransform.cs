using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonControls.FileTypes.RigidModel.Transforms
{
    [Serializable]
    public struct RmvTransform
    {
        public RmvVector3 Pivot;
        public RmvMatrix3x4 Matrix0;
        public RmvMatrix3x4 Matrix1;
        public RmvMatrix3x4 Matrix2;

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
