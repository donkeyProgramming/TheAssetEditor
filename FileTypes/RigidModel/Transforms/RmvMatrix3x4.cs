using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Transforms
{
    public struct RmvMatrix3x4
    {
        RmvVector4 Row0;
        RmvVector4 Row1;
        RmvVector4 Row2;

        public bool IsIdentity()
        {

            if (Row0.X == 1 && Row0.Y == 0 && Row0.Z == 0 && Row0.W == 0)
            {
                if (Row1.X == 0 && Row1.Y == 1 && Row1.Z == 0 && Row1.W == 0)
                {
                    if (Row2.X == 0 && Row2.Y == 0 && Row2.Z == 1 && Row2.W == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
