using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.RigidModel.Types;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IDrawableItem
    {
        public IMaterial Material { get; set; }
        public MeshObject Geometry { get; set; }
        public RmvCommonHeader CommonHeader { get; set; }

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;
        ResourceLibary _resourceLib;

        public string OriginalFilePath { get; set; }
        public int OriginalPartIndex { get; internal set; }
        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }
        public string AttachmentPointName { get; set; } = "";
        public SkeletonBoneAnimationResolver AttachmentBoneResolver { get; set; } = null;

        public bool DisplayBoundingBox { get; set; } = false;
        public bool DisplayPivotPoint { get; set; } = false;

        public override Matrix ModelMatrix { get => base.ModelMatrix; set => UpdateModelMatrix(value); }
        public PbrShader Effect { get; private set; }
        public int LodIndex { get; set; } = -1;


        bool _isSelectable = true;
        public bool IsSelectable { get => _isSelectable; set => SetAndNotifyWhenChanged(ref _isSelectable, value); }
        public bool ReduceMeshOnLodGeneration { get; set; } = true;


        private void UpdateModelMatrix(Matrix value)
        {
            base.ModelMatrix = value;
            RenderMatrix = value;
        }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }


        public AnimationPlayer AnimationPlayer;
        private RenderEngineComponent _renderEngineComponent;

        private Rmv2MeshNode()
        { }

        public Rmv2MeshNode(RmvCommonHeader commonHeader, MeshObject meshObject, IMaterial material, AnimationPlayer animationPlayer, RenderEngineComponent renderEngineComponent, PbrShader shader = null)
        {
            CommonHeader = commonHeader;
            Material = material;
            AnimationPlayer = animationPlayer;
            _renderEngineComponent = renderEngineComponent;
            Name = Material.ModelName;
            Geometry = meshObject;

            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            Effect = shader;
        }

        public void Initialize(ResourceLibary resourceLib)
        {
            _resourceLib = resourceLib;
            if (_resourceLib != null && Effect == null)
                CreateShader();
        }

        void CreateShader()
        {
            if (_renderEngineComponent.MainRenderFormat == Rendering.RenderFormats.MetalRoughness)
                Effect = new PbrShader_MetalRoughness(_resourceLib);
            else
                Effect = new PbrShader_SpecGloss(_resourceLib);

            Texture2D diffuse = LoadTexture(TextureType.Diffuse);
            Texture2D baseColour = LoadTexture(TextureType.BaseColour);
            Texture2D specTexture = LoadTexture(TextureType.Specular);
            Texture2D normalTexture = LoadTexture(TextureType.Normal);
            Texture2D glossTexture = LoadTexture(TextureType.Gloss);
            Texture2D materialTexture = LoadTexture(TextureType.MaterialMap);

            Effect.SetTexture(diffuse, TextureType.Diffuse);
            Effect.SetTexture(baseColour, TextureType.BaseColour);
            Effect.SetTexture(specTexture, TextureType.Specular);
            Effect.SetTexture(normalTexture, TextureType.Normal);
            Effect.SetTexture(glossTexture, TextureType.Gloss);
            Effect.SetTexture(materialTexture, TextureType.MaterialMap);
        }

        Texture2D LoadTexture(TextureType type, bool forceRefreshTexture = false)
        {
            var texture = Material.GetTexture(type);
            if (texture == null)
                return null;

            return _resourceLib.LoadTexture(texture.Value.Path, forceRefreshTexture);

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

        public void UpdateTexture(string path, TextureType texureType, bool forceRefreshTexture = false)
        {
            Material.SetTexture(texureType, path);

            var texture = LoadTexture(texureType, forceRefreshTexture);
            Effect.SetTexture(texture, texureType);
        }

        public void UseTexture(TextureType texureType, bool value)
        {
            Effect.UseTexture(value, texureType);
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (Effect == null || renderEngine.MainRenderFormat != Effect.RenderFormat)
                CreateShader();

            Matrix[] data = new Matrix[256];
            for (int i = 0; i < 256; i++)
                data[i] = Matrix.Identity;

            if (AnimationPlayer != null)
            {
                var frame = AnimationPlayer.GetCurrentAnimationFrame();
                if (frame != null)
                {
                    for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                        data[i] = frame.BoneTransforms[i].WorldTransform;
                }
            }

            Effect.SetAnimationParameters(data, Geometry.WeightCount);
            Effect.SetScaleMult(ScaleMult);
            Effect.UseAnimation = AnimationPlayer.IsEnabled;

            if (AttachmentBoneResolver != null)
                parentWorld = parentWorld * AttachmentBoneResolver.GetWorldTransformIfAnimating();

            Effect.UseAlpha = Material.AlphaMode == AlphaMode.Transparent;

            var modelWithOffset = ModelMatrix * Matrix.CreateTranslation(Material.PivotPoint);
            RenderMatrix = modelWithOffset;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = modelWithOffset * parentWorld, Shader = Effect });

            if (DisplayPivotPoint)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new LocatorRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), Material.PivotPoint, 1));

            if (DisplayBoundingBox)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new BoundingBoxRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), Geometry.BoundingBox));
        }

        public override ISceneNode CreateCopyInstance() => new Rmv2MeshNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as Rmv2MeshNode;
            typedTarget.Material = Material.Clone();
            typedTarget.CommonHeader = CommonHeader;
            typedTarget.Position = Position;
            typedTarget.Orientation = Orientation;
            typedTarget.Scale = Scale;
            typedTarget.LodIndex = LodIndex;
            typedTarget.ReduceMeshOnLodGeneration = ReduceMeshOnLodGeneration;
            typedTarget.AnimationPlayer = AnimationPlayer;
            typedTarget.CommonHeader = CommonHeader;
            typedTarget.Material = Material.Clone();
            typedTarget.Geometry = Geometry.Clone();
            typedTarget._resourceLib = _resourceLib;
            typedTarget._renderEngineComponent = _renderEngineComponent;
            //warhammer 2 compat
            if (typedTarget.Effect != null)
            {
                typedTarget.Effect = Effect.Clone() as PbrShader_MetalRoughness;
            }
            typedTarget.Geometry = Geometry.Clone();
            typedTarget.OriginalFilePath = OriginalFilePath;
            typedTarget.OriginalPartIndex = OriginalPartIndex;
            typedTarget.ScaleMult = ScaleMult;
            base.CopyInto(target);
        }

        public void UpdatePivotPoint(Vector3 newPiv)
        {
            Material.PivotPoint = newPiv;
        }

        public Dictionary<TextureType, string> GetTextures()
        {
            var enumCollection = Enum.GetValues(typeof(TextureType));
            var output = new Dictionary<TextureType, string>();

            foreach (var enumValue in enumCollection)
            {
                var texture = Material.GetTexture((TextureType)enumValue);
                if (texture != null && texture.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(texture.Value.Path) == false)
                        output[(TextureType)enumValue] = texture.Value.Path;
                }
            }

            return output;
        }
    }



}
