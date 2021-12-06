using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommonControls.FileTypes.RigidModel.Transforms
{
    [StructLayout(LayoutKind.Sequential, Size = 24)]
    [Serializable]
    public struct RvmBoundingBox
    {
        public float MinimumX;
        public float MinimumY;
        public float MinimumZ;
        public float MaximumX;
        public float MaximumY;
        public float MaximumZ;

        public void UpdateBoundingBox(BoundingBox newBB)
        {

            MinimumX = newBB.Min.X;
            MinimumY = newBB.Min.Y;
            MinimumZ = newBB.Min.Z;

            MaximumX = newBB.Max.X;
            MaximumY = newBB.Max.Y;
            MaximumZ = newBB.Max.Z;
        }
    }



}
