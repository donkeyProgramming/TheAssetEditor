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
        public IRmvMaterial RmvMaterial { get; set; }
        public MeshObject Geometry { get; set; }

        public Vector3 Position { get; set { field = value; UpdateMatrix(); } } = Vector3.Zero;
        public Vector3 Scale { get; set { field = value; UpdateMatrix(); } } = Vector3.One;
        public Quaternion Orientation { get;  set { field = value; UpdateMatrix(); } } = Quaternion.Identity;
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

        /// <summary>Extra outer-world factor composed AFTER the resolved attachment bone, applied
        /// only when <see cref="AttachmentBoneResolver"/> is set. Exists because flat scene designs
        /// (the CSC editor) push a leaf's full world transform directly into <see cref="ModelMatrix"/>
        /// instead of relying on the engine's ancestor-chain accumulation (needed so picking, which
        /// reads a node's own ModelMatrix with no parent chain, still works) - for an attach-resolved
        /// mesh that world belongs here, one step further out than the bone, not in ModelMatrix
        /// (which would then be applied on the wrong side of the bone transform). Identity - a
        /// no-op - for every other caller (e.g. Kitbasher's normal nested scenegraph, where the
        /// bone's outer world already arrives correctly through the ordinary parentWorld chain).</summary>
        public Matrix AttachmentOuterWorld { get; set; } = Matrix.Identity;

    
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
            var frame = AnimationPlayer?.GetCurrentAnimationFrame();

            // Bone attachment: either a variantmesh attach_point (a bone on a DIFFERENT model's
            // skeleton this prop is attached to) or RMV2's own rigid-bone id
            // (WeightedMaterial.MatrixIndex, AnimationMatrixOverride - how "building" destruction
            // pieces attach to their OWN skeleton regardless of vertex format). Either way the
            // resolved bone's current world transform places this mesh's own origin at the bone -
            // applied AFTER ModelMatrix/pivot (the mesh's own local content) but BEFORE the outer
            // world (AttachmentOuterWorld, then parentWorld): the bone's transform is defined in
            // the attached-to model's own local space, so it must be placed within that model's
            // world, not the other way around - getting this order backwards is what sends
            // attached meshes flying off to the wrong place under any non-trivial rotation.
            var followsBoneRigidly = AttachmentBoneResolver != null && AnimationMatrixOverride >= 0;
            var boneMatrix = AttachmentBoneResolver?.GetWorldTransformIfAnimating() ?? Matrix.Identity;

            // PivotPoint is a LOCAL-space offset (the RMV2's own "attachment point") and must be
            // composed first, before ModelMatrix - it needs to inherit whatever scale/rotation the
            // mesh itself is placed with, same as any other local geometry feature. Composing it
            // after ModelMatrix (as before) added it as a fixed, unscaled world-space nudge instead
            // - correct only for the (extremely common) PivotPoint == zero case, but wrong by
            // exactly (scale - 1) * PivotPoint whenever a CSC/scene element scales the mesh - e.g.
            // a scaled-up destruct piece with a real pivot ended up too low by the un-applied
            // portion of Scale * PivotPoint (confirmed against a real file: a 6x-scaled train piece
            // with PivotPoint.Y ~2.7 sat ~14 units too low - within rounding of PivotPoint.Y * (6 - 1)).

            var animationCapability = Material.TryGetCapability<AnimationCapability>();
            if (animationCapability != null)
            {
                var data = new Matrix[256];
                for (var i = 0; i < 256; i++)
                    data[i] = Matrix.Identity;

                if (frame != null)
                {
                    for (var i = 0; i < frame.BoneTransforms.Count(); i++)
                        data[i] = frame.BoneTransforms[i].WorldTransform;
                }

                animationCapability.AnimationTransforms = data;
                animationCapability.AnimationWeightCount = Geometry.WeightCount;
                animationCapability.ApplyAnimation = AnimationPlayer != null && AnimationPlayer.IsEnabled
                    && Geometry.VertexFormat != UiVertexFormat.Static && !followsBoneRigidly;
            }

            var modelWithOffset = Matrix.CreateTranslation(PivotPoint) * ModelMatrix * boneMatrix * AttachmentOuterWorld;
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
            typedTarget.AttachmentPointName = AttachmentPointName;
            typedTarget.ScaleMult = ScaleMult;
            typedTarget.PivotPoint = PivotPoint;

            typedTarget.RmvMaterial = RmvMaterial.Clone();
            typedTarget.AnimationMatrixOverride = AnimationMatrixOverride;
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


        public override void OnNodeAdded() => Geometry?.EnsureGraphicsResourcesCreated();
        
        public override void OnNodeRemoved() => Geometry?.RemoveGraphicsCardResources();
        
    }
}
