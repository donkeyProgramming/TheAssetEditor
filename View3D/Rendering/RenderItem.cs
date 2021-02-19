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
using View3D.Utility;

namespace View3D.Rendering
{
    public static class RenderItemHelper
    {
        public static void CreateDefaultShaders(MeshNode item, GraphicsDevice device, ResourceLibary resourceLib)
        {
            item.DefaultEffect = resourceLib.GetEffect(ShaderTypes.Phazer);
            //item.DefaultEffect = new BasicEffect(device);
            //item.DefaultEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            //item.DefaultEffect.EnableDefaultLighting();
            //item.DefaultEffect.Alpha = 1;

            item.WireframeEffect = new BasicEffect(device);
            item.WireframeEffect.DiffuseColor = Vector3.Zero;

            item.SelectedFacesEffect = new BasicEffect(device);
            item.SelectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            item.SelectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
            item.SelectedFacesEffect.EnableDefaultLighting();

            //Remove this function
            // Create skeleton node under editableMesh
            /*
                         var hasDiffuse = Textures.TryGetValue(TexureType.Diffuse, out var diffuseTexture);
            var hasSpec = Textures.TryGetValue(TexureType.Specular, out var specTexture);
            var hasNormal = Textures.TryGetValue(TexureType.Normal, out var normalTexture);
            var hasGloss = Textures.TryGetValue(TexureType.Gloss, out var glossTexture);

            _shader.Parameters["DiffuseTexture"].SetValue(diffuseTexture);
            _shader.Parameters["SpecularTexture"].SetValue(specTexture);
            _shader.Parameters["NormalTexture"].SetValue(normalTexture);
            _shader.Parameters["GlossTexture"].SetValue(glossTexture);

            _shader.Parameters["tex_cube_specular"].SetValue(m_pbrDiffuse);
            _shader.Parameters["specularBRDF_LUT"].SetValue(m_BRDF_LUT);
            _shader.Parameters["tex_cube_diffuse"].SetValue(m_pbrSpecular);
             
             */
        }

        //public static MeshNode CreateRenderItem(IGeometry geo, Vector3 position, Vector3 scale, string name, GraphicsDevice device, ResourceLibary resourceLib,  bool isEditable = true)
        //{
        //    var item = new MeshNode()
        //    {
        //        Name = name,
        //        IsEditable = isEditable,
        //        Geometry = geo,
        //        Position = position,
        //        Scale = scale,
        //        Orientation = Quaternion.Identity,
        //    };
        //    CreateDefaultShaders(item, device, resourceLib);
        //    return item;
        //}
    }
}
