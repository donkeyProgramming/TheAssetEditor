using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Animation;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Rendering.RenderItems;
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
        public IRmvMaterial Material { get; set; }
        public MeshObject Geometry { get; set; }
        public RmvCommonHeader CommonHeader { get; set; }

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public string OriginalFilePath { get; set; }
        public int OriginalPartIndex { get; internal set; }
        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }
        public string AttachmentPointName { get; set; } = "";
     
        public bool DisplayBoundingBox { get; set; } = false;
        public bool DisplayPivotPoint { get; set; } = false;

        public override Matrix ModelMatrix { get => base.ModelMatrix; set => UpdateModelMatrix(value); }
        public CapabilityMaterial Effect { get; set; }
        public int LodIndex { get; set; } = -1;

        bool _isSelectable = true;
        public bool IsSelectable { get => _isSelectable; set => SetAndNotifyWhenChanged(ref _isSelectable, value); }
        public bool ReduceMeshOnLodGeneration { get; set; } = true;

        public AnimationPlayer? AnimationPlayer { get; set; }                               // This is a hack - remove at some point
        public SkeletonBoneAnimationResolver? AttachmentBoneResolver { get; set; } = null;  // This is a hack - remove at some point

        private Rmv2MeshNode()
        { }

        public Rmv2MeshNode(RmvCommonHeader commonHeader, MeshObject meshObject, IRmvMaterial material, AnimationPlayer animationPlayer, CapabilityMaterial shader)
        {
            CommonHeader = commonHeader;
            Material = material;
            AnimationPlayer = animationPlayer;
            Geometry = meshObject;
            Effect = shader;

            Name = Material.ModelName;
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

        public Vector3 GetObjectCentre() => MathUtil.GetCenter(Geometry.BoundingBox) + Position;
       
        public void UpdateTexture(string path, TextureType textureType, bool forceRefreshTexture = false)
        {
           // Material.SetTexture(textureType, path);
           // _resourceLib.LoadTexture(path, forceRefreshTexture);
           //
           // var sharedCapability = Effect.GetCapability<DefaultCapability>();
           // if (sharedCapability != null)
           // {
           //     sharedCapability.SetTexturePath(textureType, path);
           //     sharedCapability.SetTextureUsage(textureType, true);
           // }
        }

        public void UseTexture(TextureType textureType, bool value)
        {
            //var sharedCapability = Effect.GetCapability<DefaultCapability>();
            //if (sharedCapability != null)
            //{
            //    sharedCapability.SetTextureUsage(textureType, value);
            //}
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var animationCapability = Effect.GetCapability<AnimationCapability>();
            if (animationCapability != null)
            {
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

                animationCapability.AnimationTransforms = data;
                animationCapability.AnimationWeightCount = Geometry.WeightCount;
                animationCapability.ApplyAnimation = AnimationPlayer != null && AnimationPlayer.IsEnabled;
            }

            var sharedCapability = Effect.GetCapability<DefaultCapabilityMetalRough>();
            if (sharedCapability != null)
            {
                sharedCapability.ScaleMult = ScaleMult;
                sharedCapability.UseAlpha = Material.AlphaMode == AlphaMode.Transparent;
            }

            if (AttachmentBoneResolver != null)
                parentWorld = parentWorld * AttachmentBoneResolver.GetWorldTransformIfAnimating();

            var modelWithOffset = ModelMatrix * Matrix.CreateTranslation(Material.PivotPoint);
            RenderMatrix = modelWithOffset;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeometryRenderItem(Geometry, Effect, modelWithOffset * parentWorld));

            if (DisplayPivotPoint)
                renderEngine.AddRenderLines(LineHelper.AddLocator(Material.PivotPoint, 1, Color.Red));

            if (DisplayBoundingBox)
                renderEngine.AddRenderLines(LineHelper.AddBoundingBox(Geometry.BoundingBox, Color.Red));
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

        private void UpdateModelMatrix(Matrix value)
        {
            base.ModelMatrix = value;
            RenderMatrix = value;
        }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }
    }
}
