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

        List<RenderItem> _renderItems = new List<RenderItem>();

        public IEnumerable<RenderItem> RenderItems => _renderItems;

        public void AddObject(RenderItem item)
        {
            _renderItems.Add(item);
        }

        public void RemoveObject(RenderItem item)
        {
            _renderItems.Remove(item);
        }

        public bool ContainsObject(RenderItem item)
        { 
            return _renderItems.Contains(item);
        }

        public RenderItem SelectObject(Ray ray)
        {
            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;

            foreach (var item in _renderItems)
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
            foreach (var item in _renderItems)
            {
                if (GeometryIntersection.IntersectObject(frustrum, item))
                    output.Add(item);
            }

            return output;
        }
    }
}
