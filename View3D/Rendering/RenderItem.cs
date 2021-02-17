using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Gizmo;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Rendering
{
    public static class RenderItemHelper
    {
        public static void CreateDefaultShaders(MeshNode item, GraphicsDevice device)
        {
            item.DefaultEffect = new BasicEffect(device);
            item.DefaultEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            item.DefaultEffect.EnableDefaultLighting();
            item.DefaultEffect.Alpha = 1;

            item.WireframeEffect = new BasicEffect(device);
            item.WireframeEffect.DiffuseColor = Vector3.Zero;

            item.SelectedFacesEffect = new BasicEffect(device);
            item.SelectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            item.SelectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
            item.SelectedFacesEffect.EnableDefaultLighting();
        }

        public static MeshNode CreateRenderItem(IGeometry geo, Vector3 position, Vector3 scale, string name, GraphicsDevice device, bool isEditable = true)
        {
            var item = new MeshNode()
            {
                Name = name,
                IsEditable = isEditable,
                Geometry = geo,
                Position = position,
                Scale = scale,
                Orientation = Quaternion.Identity,
            };
            CreateDefaultShaders(item, device);
            return item;
        }
    }
}
