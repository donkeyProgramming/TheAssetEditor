using Filetypes.RigidModel;
using Filetypes.RigidModel.Vertex;
using MeshDecimator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
        ResourceLibary _resourceLib;

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
            _resourceLib = resourceLib;
            Geometry = geometry;
            if (Geometry == null)
                Geometry = new Rmv2Geometry(rmvSubModel, resourceLib.GraphicsDevice);
            AnimationPlayer = animationPlayer;

            Name = rmvSubModel.Header.ModelName;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            Effect = new PbrShader(resourceLib);
            var diffuse = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Diffuse).Value.Path);
            var specTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Specular).Value.Path);
            var normalTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Normal).Value.Path);
            var glossTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Gloss).Value.Path);
            
            (Effect as IShaderTextures).SetTexture(diffuse, TexureType.Diffuse);
            (Effect as IShaderTextures).SetTexture(specTexture, TexureType.Specular);
            (Effect as IShaderTextures).SetTexture(normalTexture, TexureType.Normal);
            (Effect as IShaderTextures).SetTexture(glossTexture, TexureType.Gloss);
        }


        public IShader Effect { get; set; }
        public int LodIndex { get; set; } = -1;

        internal RmvSubModel CreateRmvSubModel()
        {
            var newSubModel = MeshModel.Clone();
            var typedGeo = (Geometry as Rmv2Geometry);
            newSubModel.Mesh.IndexList = typedGeo.GetIndexBuffer().ToArray();
            newSubModel.Mesh.VertexList = new DefaultVertex[typedGeo.VertexCount()];

            var vert = typedGeo.GetVertexById(0);
            /*
             
                             case VertexFormat.Default:
                    return ByteHelper.GetSize(typeof(DefaultVertex.Data));
                case VertexFormat.Weighted:
                    return ByteHelper.GetSize(typeof(WeightedVertex.Data));
                case VertexFormat.Cinematic:
                    return ByteHelper.GetSize(typeof(CinematicVertex.Data));
             
             */

            throw new NotImplementedException();
        }

        public IGeometry Geometry { get; set; }
        public bool IsSelectable { get; set; } = true;

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

        public void UpdateTexture(string path, TexureType texureType)
        {
            var texture = _resourceLib.LoadTexture(path);
            (Effect as IShaderTextures).SetTexture(texture, texureType);

            for (int i = 0; i < MeshModel.Textures.Count; i++)
            {
                if (MeshModel.Textures[i].TexureType == texureType)
                {
                    var tex = MeshModel.Textures[i];
                    tex.Path = path;
                    MeshModel.Textures[i] = tex;
                    break;
                }
            }
        }

        public void UseTexture(TexureType texureType, bool value)
        {
            (Effect as IShaderTextures).UseTexture(value, texureType);
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
                tetureEffect.UseAlpha = MeshModel.Mesh.AlphaSettings.Mode == AlphaMode.Alpha_Test;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = ModelMatrix, Shader = Effect });
        }

        public override ISceneNode Clone()
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
                IsSelectable= IsSelectable,
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
