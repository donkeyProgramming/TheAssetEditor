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
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IUpdateable, IDrawableItem
    {
        public RmvSubModel MeshModel { get; set; }


        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }


        public AnimationPlayer AnimationPlayer;

        private Rmv2MeshNode()
        { }


        public Rmv2MeshNode(RmvSubModel rmvSubModel, ResourceLibary resourceLib, AnimationPlayer animationPlayer, IGeometry geometry = null)
        {
            MeshModel = rmvSubModel;
            Geometry = geometry;
            if (Geometry == null)
                Geometry = new Rmv2Geometry(rmvSubModel, resourceLib.GraphicsDevice);
            AnimationPlayer = animationPlayer;

            Name = rmvSubModel.Header.ModelName;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            Effect = new NewShader(resourceLib);
            //var diffuse = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Diffuse).Path);
            //var specTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Specular).Path);
            var normalTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Normal).Path);
            //var glossTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Gloss).Path);
            //
            //(Effect as PbrShader).SetTexture(diffuse, TexureType.Diffuse);
            //(Effect as PbrShader).SetTexture(specTexture, TexureType.Specular);
            (Effect as IShaderTextures).SetTexture(normalTexture, TexureType.Normal);
            //(Effect as PbrShader).SetTexture(glossTexture, TexureType.Gloss);
        }


        public IShader Effect { get; set; }
        public int LodIndex { get; set; } = -1;
        public IGeometry Geometry { get; set; }

        public void Update(GameTime time)
        {

        }

        public Rmv2ModelNode GetParentModel()
        {
            var parent = Parent;
            while (parent != null)
            {
                if (parent is Rmv2ModelNode modelNode)
                    return modelNode;
                parent = parent.Parent;
            }

            return null;
        }

        public Vector3 GetObjectCenter()
        {
            return MathUtil.GetCenter(Geometry.BoundingBox) + Position;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (Effect is IShaderAnimation animationEffect)
            {
                Matrix[] data = new Matrix[256];
                for (int i = 0; i < 256; i++)
                    data[i] = Matrix.Identity;

                if (AnimationPlayer != null)
                {
                    var frame = AnimationPlayer.GetCurrentFrame();
                    if (frame != null)
                    {
                        for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                            data[i] = frame.BoneTransforms[i].WorldTransform;
                    }
                }

                animationEffect.SetAnimationParameters(data, 4);
                animationEffect.UseAnimation = AnimationPlayer.IsEnabled;
            }

            if (Effect is IShaderTextures tetureEffect)
            {
                tetureEffect.UseAlpha = false;
            }

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = ModelMatrix, Shader = Effect });
        }

        public override SceneNode Clone()
        {
            var newItem = new Rmv2MeshNode()
            {
                Geometry = Geometry.Clone(),
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                Parent = Parent,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                LodIndex = LodIndex,
                Name = Name + " - Clone",
                AnimationPlayer = AnimationPlayer,
                MeshModel = MeshModel
            };
            newItem.Effect = Effect.Clone();
            return newItem;
        }
    }
}
