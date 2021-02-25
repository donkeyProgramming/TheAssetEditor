using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Utility
{
    public class MathUtil
    {
       public static Vector3 GetCenter(BoundingBox box)
        {
            Vector3 finalPos = Vector3.Zero;
            var corners = box.GetCorners();
            foreach (var corner in corners)
                finalPos += corner;

            return finalPos / corners.Length;
        }
    }
}
