using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;

namespace View3D.Components.Component.Selection
{
    public static class PickingUtil
    {
        public static RenderItem SelectObject(Ray ray, SceneManager scene)
        {
            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;

            foreach (var item in scene.RenderItems)
            {
                var distance = item.Geometry.IntersectObject(ray, item.ModelMatrix);
                if (distance != null)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance.Value;
                        bestItem = item;
                    }
                }
            }

            return bestItem;
        }

        public static List<RenderItem> SelectObjects(BoundingFrustum frustrum, SceneManager scene)
        {
            List<RenderItem> output = new List<RenderItem>();
            foreach (var item in scene.RenderItems)
            {
                if (item.Geometry.IntersectObject(frustrum, item.ModelMatrix))
                    output.Add(item);
            }

            return output;
        }
    }
}
