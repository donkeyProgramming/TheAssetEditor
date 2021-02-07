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

namespace View3D.Rendering
{
    public class RenderItem : ITransformable
    {
        public BasicEffect DefaultEffect { get; set; }
        public BasicEffect WireframeEffect { get; set; }
        public BasicEffect SelectedFacesEffect { get; set; }

        public bool IsEditable { get; set; }

        public Matrix ModelMatrix { get; private set; } = Matrix.Identity;
        public IGeometry Geometry { get; set; }
        public string Name { get; set; } = "";

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }

        public RenderItem(IGeometry geo, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Geometry = geo;
            _position = position;
            _orientation = rotation;
            _scale = scale;
            UpdateMatrix();
        }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }

        public void DrawWireframeOverlay(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams)
        {
            WireframeEffect.Projection = shaderParams.Projection;
            WireframeEffect.View = shaderParams.View;
            WireframeEffect.World = ModelMatrix;
            Geometry.ApplyMesh(WireframeEffect, device);
        }

        public void DrawSelectedFaces(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams, List<int> faces)
        {
            SelectedFacesEffect.Projection = shaderParams.Projection;
            SelectedFacesEffect.View = shaderParams.View;
            SelectedFacesEffect.World = ModelMatrix;
            Geometry.ApplyMeshPart(SelectedFacesEffect, device, faces);
        }

        public void DrawBasic(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams)
        {
            DefaultEffect.Projection = shaderParams.Projection;
            DefaultEffect.View = shaderParams.View;
            DefaultEffect.World = ModelMatrix;
            Geometry.ApplyMesh(DefaultEffect, device);
        }

        public RenderItem Clone()
        {
            var newItem = new RenderItem(Geometry.Clone(), Position, Orientation, Scale);
            newItem.DefaultEffect = (BasicEffect)DefaultEffect.Clone();
            newItem.WireframeEffect = (BasicEffect)WireframeEffect.Clone();
            newItem.SelectedFacesEffect = (BasicEffect)SelectedFacesEffect.Clone();
            newItem.Name = Name + " - Clone";
            return newItem;
        }
    }

    public static class RenderItemHelper
    {
        public static void CreateDefaultShaders(RenderItem item, GraphicsDevice device, ContentManager content)
        {
            item.DefaultEffect = new BasicEffect(device);
            item.DefaultEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            item.DefaultEffect.EnableDefaultLighting(); 

            item.WireframeEffect = new BasicEffect(device);
            item.WireframeEffect.DiffuseColor = Vector3.Zero;

            item.SelectedFacesEffect = new BasicEffect(device);
            item.SelectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            item.SelectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
            item.SelectedFacesEffect.EnableDefaultLighting();
        }

        public static RenderItem CreateRenderItem(IGeometry geo, Vector3 position, Vector3 scale, string name, WpfGame game, bool isEditable = true)
        {
            var item = new RenderItem(geo, position, Quaternion.Identity, scale);
            item.Name = name;
            item.IsEditable = isEditable;
            CreateDefaultShaders(item, game.GraphicsDevice, game.Content);
            return item;
        }
    }
}
