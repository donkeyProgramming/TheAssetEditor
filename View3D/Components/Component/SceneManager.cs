using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Components.Component
{
    public class SceneManager : BaseComponent
    {
        public SceneManager(WpfGame game) : base(game) { }

        public List<RenderItem> RenderItems = new List<RenderItem>();

        public RenderItem SelectObject(Ray ray)
        {
            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;

            foreach (var item in RenderItems)
            {
                var distance = GeometryIntersection.IntersectObject(ray, item);
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

        public List<RenderItem> SelectObjects(BoundingFrustum frustrum)
        {
            List<RenderItem> output = new List<RenderItem>();
            foreach (var item in RenderItems)
            {
                if (GeometryIntersection.IntersectObject(frustrum, item))
                    output.Add(item);
            }

            return output;
        }
    }
}
