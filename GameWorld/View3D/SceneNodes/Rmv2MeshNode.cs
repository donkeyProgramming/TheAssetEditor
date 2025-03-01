using System;
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
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IDrawableItem
    {
        private Quaternion _orientation = Quaternion.Identity;
        private Vector3 _position = Vector3.Zero;
        private Vector3 _scale = Vector3.One;

        public IRmvMaterial RmvMaterial { get; set; }
        public MeshObject Geometry { get; set; }
        public RmvCommonHeader CommonHeader { get; set; }

 
        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }
        public Vector3 PivotPoint { get; set; }

        public string AttachmentPointName { get; set; } = "";
        public int AnimationMatrixOverride { get; set; } = -1;

        public bool DisplayBoundingBox { get; set; } = false;
        public bool DisplayPivotPoint { get; set; } = false;
        public bool ReduceMeshOnLodGeneration { get; set; } = true;

        public override Matrix ModelMatrix { get => base.ModelMatrix; set => UpdateModelMatrix(value); }
        public CapabilityMaterial Material { get; set; }
       

        bool _isSelectable = true;
        public bool IsSelectable { get => _isSelectable; set => SetAndNotifyWhenChanged(ref _isSelectable, value); }

        public AnimationPlayer? AnimationPlayer { get; set; }                               // This is a hack - remove at some point
        public SkeletonBoneAnimationResolver? AttachmentBoneResolver { get; set; } = null;  // This is a hack - remove at some point

    
        public Rmv2MeshNode(MeshObject meshObject, IRmvMaterial material, CapabilityMaterial shader, AnimationPlayer animationPlayer)
        {
            RmvMaterial = material;
            AnimationPlayer = animationPlayer;
            Geometry = meshObject;
            Material = shader;

            Name = material.ModelName;
            PivotPoint = material.PivotPoint;

            if(material != null && material is WeightedMaterial weightedMaterial)
                AnimationMatrixOverride = weightedMaterial.MatrixIndex;
        }

        private Rmv2MeshNode() { }
       
        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var animationCapability = Material.TryGetCapability<AnimationCapability>();
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
                animationCapability.ApplyAnimation = AnimationPlayer != null && AnimationPlayer.IsEnabled && Geometry.VertexFormat != UiVertexFormat.Static;
            }

            if (AttachmentBoneResolver != null)
                parentWorld = parentWorld * AttachmentBoneResolver.GetWorldTransformIfAnimating();

            var modelWithOffset = ModelMatrix * Matrix.CreateTranslation(PivotPoint);
            RenderMatrix = modelWithOffset;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeometryRenderItem(Geometry, Material, modelWithOffset * parentWorld));

            if (DisplayPivotPoint)
                renderEngine.AddRenderLines(LineHelper.AddLocator(PivotPoint, 1, Color.Red));

            if (DisplayBoundingBox)
                renderEngine.AddRenderLines(LineHelper.AddBoundingBox(Geometry.BoundingBox, Color.Red, PivotPoint));
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

        public override ISceneNode CreateCopyInstance() => new Rmv2MeshNode();

        public override void CopyInto(ISceneNode target)
        {
            CopyInto(target, true);
            base.CopyInto(target);
        }

        public void CopyInto(ISceneNode target, bool includeMesh)
        {
            if (target is not Rmv2MeshNode typedTarget)
                throw new Exception("Error casting");

            typedTarget.Position = Position;
            typedTarget.Orientation = Orientation;
            typedTarget.Scale = Scale;
            typedTarget.ReduceMeshOnLodGeneration = ReduceMeshOnLodGeneration;
            typedTarget.AnimationPlayer = AnimationPlayer;
            typedTarget.ScaleMult = ScaleMult;
            typedTarget.PivotPoint = PivotPoint;

            typedTarget.RmvMaterial = RmvMaterial.Clone();
            typedTarget.AnimationMatrixOverride = AnimationMatrixOverride;
            typedTarget.Geometry = Geometry.Clone();
            typedTarget.Material = Material.Clone();
           
            if(includeMesh)
                typedTarget.Geometry = Geometry.Clone();

            base.CopyInto(target);
        }

        void UpdateModelMatrix(Matrix value)
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
