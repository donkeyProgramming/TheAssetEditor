using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.Transforms
{
    [StructLayout(LayoutKind.Sequential, Size = 24)]
    public struct RvmBoundingBox
    {
        public float MinimumX;
        public float MinimumY;
        public float MinimumZ;
        public float MaximumX;
        public float MaximumY;
        public float MaximumZ;

        //public override string ToString()
        //{
        //    return $"x(min:{MinimumX} max:{MaximumX}) y(min:{MinimumY} max:{MaximumY}) z(min:{MinimumZ} max:{MaximumZ})";
        //}
    }
}
