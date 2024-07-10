using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.Rendering.Shading;
using GameWorld.Core.Utility;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IDrawableItem
    {
        public IMaterial Material { get; set; }
        public MeshObject Geometry { get; set; }
        public RmvCommonHeader CommonHeader { get; set; }

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        ResourceLibrary _resourceLib;
        private RenderEngineComponent _renderEngineComponent;

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


        public AnimationPlayer AnimationPlayer { get; set; }
       

        private Rmv2MeshNode()
        { }

        public Rmv2MeshNode(RmvCommonHeader commonHeader, MeshObject meshObject, IMaterial material, AnimationPlayer animationPlayer, RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;

            CommonHeader = commonHeader;
            Material = material;
            AnimationPlayer = animationPlayer;
           
            Name = Material.ModelName;
            Geometry = meshObject;

            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;
        }

        public void Initialize(ResourceLibrary resourceLib)
        {
            _resourceLib = resourceLib;
            if (_resourceLib != null && Effect == null)
                CreateShader();
        }

        void CreateShader()
        {
            Effect = new PbrShader(_resourceLib, _renderEngineComponent.MainRenderFormat);
            foreach (TextureType textureType in Enum.GetValues(typeof(TextureType)))
            {
                var texture = Material.GetTexture(textureType);
                if (texture != null)
                {
                    _resourceLib.LoadTexture(texture.Value.Path);
                    Effect.SetTexture(textureType, texture.Value.Path);
                }
            }
        }

        public Rmv2ModelNode? GetParentModel()
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

        public Vector3 GetObjectCentre()
        {
            return MathUtil.GetCenter(Geometry.BoundingBox) + Position;
        }

        public void UpdateTexture(string path, TextureType textureType, bool forceRefreshTexture = false)
        {
            Material.SetTexture(textureType, path);
            _resourceLib.LoadTexture(path, forceRefreshTexture);
            Effect.SetTexture(textureType, path);
        }

        public void UseTexture(TextureType textureType, bool value) => Effect.UseTexture(textureType, value);

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (Effect == null || renderEngine.MainRenderFormat != Effect.RenderFormat)
                CreateShader();

            var data = new Matrix[256];
            for (var i = 0; i < 256; i++)
                data[i] = Matrix.Identity;

            if (AnimationPlayer != null)
            {
                var frame = AnimationPlayer.GetCurrentAnimationFrame();
                if (frame != null)
                {
                    for (var i = 0; i < frame.BoneTransforms.Count(); i++)
                        data[i] = frame.BoneTransforms[i].WorldTransform;
                }
            }

            Effect.AnimationTransforms = data;
            Effect.AnimationWeightCount = Geometry.WeightCount;
            Effect.UseAnimation = AnimationPlayer.IsEnabled;
            Effect.ScaleMult = ScaleMult;
            Effect.UseAlpha = Material.AlphaMode == AlphaMode.Transparent;

            if (AttachmentBoneResolver != null)
                parentWorld = parentWorld * AttachmentBoneResolver.GetWorldTransformIfAnimating();

            var modelWithOffset = ModelMatrix * Matrix.CreateTranslation(Material.PivotPoint);
            RenderMatrix = modelWithOffset;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeometryRenderItem(Geometry, Effect, modelWithOffset * parentWorld));

            if (DisplayPivotPoint)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new LocatorRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), Material.PivotPoint, 1));

            if (DisplayBoundingBox)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new BoundingBoxRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), Geometry.BoundingBox));
        }

        public override ISceneNode CreateCopyInstance() => new Rmv2MeshNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as Rmv2MeshNode;
            if (typedTarget == null)
                throw new Exception("Error casting");
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
           
    
            typedTarget.Effect = Effect.Clone();
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
