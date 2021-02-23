using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class MeshNode : GroupNode, ITransformable, IEditableGeometry, ISelectable, IUpdateable, IDrawableItem
    {
        public AnimationPlayer AnimationPlayer;

        private MeshNode()
        { }

        public MeshNode(IGeometry geo, string name, AnimationPlayer animationPlayer, PbrShader shader)
        {
            Geometry = geo;
            AnimationPlayer = animationPlayer;

            Name = name;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            DefaultEffect = shader;
        }

        public MeshNode(RmvSubModel rmvSubModel, GraphicsDevice device, ResourceLibary resourceLib, AnimationPlayer animationPlayer)
        {
            Geometry = new Rmv2Geometry(rmvSubModel, device);
            AnimationPlayer = animationPlayer;

            Name = rmvSubModel.Header.ModelName;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            DefaultEffect = new PbrShader(resourceLib);
            var diffuse = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Diffuse).Path);
            var specTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Specular).Path);
            var normalTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Normal).Path);
            var glossTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Gloss).Path);
            DefaultEffect.SetTexture(diffuse, TexureType.Diffuse);
            DefaultEffect.SetTexture(specTexture, TexureType.Specular);
            DefaultEffect.SetTexture(normalTexture, TexureType.Normal);
            DefaultEffect.SetTexture(glossTexture, TexureType.Gloss);
        }

        public PbrShader DefaultEffect { get; set; }
        public int LodIndex { get; set; } = -1;
        public IGeometry Geometry { get; set; }

        public void Update(GameTime time)
        {
            Matrix[] data = new Matrix[256];
            for (int i = 0; i < 256; i++)
                data[i] = Matrix.Identity;

            var player = AnimationPlayer;
            if (player != null)
            {
                var frame = player.GetCurrentFrame();
                if (frame != null)
                {
                    for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                        data[i] = frame.BoneTransforms[i].WorldTransform;
                }
            }

            DefaultEffect.SetAnimationParameters(data, 4);
            DefaultEffect.UseAlpha = false;
            DefaultEffect.UseAnimation = AnimationPlayer.IsEnabled;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = ModelMatrix, Shader = DefaultEffect });
        }



        public override SceneNode Clone()
        {
            var newItem = new MeshNode()
            {
                Geometry = Geometry.Clone(),
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                LodIndex = LodIndex,
                Name = Name + " - Clone",
                AnimationPlayer = AnimationPlayer,
            };
            newItem.DefaultEffect = (PbrShader)DefaultEffect.Clone();
            return newItem;
        }
    }
}
