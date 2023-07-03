using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using View3D.Animation;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.RenderItems;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public interface ISkeletonProvider
    { 
        GameSkeleton Skeleton { get;  }
    }

    public class SkeletonNode : GroupNode, ISkeletonProvider, IDrawableItem, IDisposable
    {
        LineMeshRender _lineRenderer;

        public Color NodeColour = Color.Black;
        public Color SelectedNodeColour = Color.Red;
        public Vector3 LineColour = new Vector3(0, 0, 0);

        public int? SelectedBoneIndex { get; set; }
        public float SkeletonScale { get; set; } = 1;
        public float SelectedBoneScaleMult { get; set; } = 1.5f;

        public GameSkeleton Skeleton { get; set; }

        public SkeletonNode(ResourceLibary resourceLibary, GameSkeleton skeleton, string name = "Skeleton") : base(name)
        {
            _lineRenderer = new LineMeshRender(resourceLibary);
            Skeleton = skeleton;
        }

        protected SkeletonNode() { }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            Name = "Skeleton ";
            if (Skeleton != null)
                Name = Skeleton.SkeletonName;

            if (IsVisible && Skeleton != null)
            {
                _lineRenderer.Clear(); 
                for (int i = 0; i < Skeleton.BoneCount; i++)
                {
                    float scale = SkeletonScale;
                    Color drawColour = NodeColour;
                    if (SelectedBoneIndex.HasValue && SelectedBoneIndex.Value == i)
                    {
                        drawColour = SelectedNodeColour;
                        scale *= SelectedBoneScaleMult;
                    }

                    var boneMatrix = Skeleton.GetAnimatedWorldTranform(i);
                    _lineRenderer.AddCube(Matrix.CreateScale(scale) * Matrix.CreateScale(0.05f) * boneMatrix * Matrix.CreateScale(ScaleMult) * parentWorld, drawColour);
                    
                    var parentIndex = Skeleton.GetParentBoneIndex(i);
                    if (parentIndex != -1)
                    {
                        var currentBoneMatrix = boneMatrix * Matrix.CreateScale(ScaleMult);
                        var parentBoneMatrix = Skeleton.GetAnimatedWorldTranform(parentIndex) * Matrix.CreateScale(ScaleMult);
                        _lineRenderer.AddLine(Vector3.Transform(currentBoneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld));
                    }
                }

                renderEngine.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _lineRenderer, ModelMatrix = Matrix.Identity });
            }
        }

        public override ISceneNode CreateCopyInstance() => new SkeletonNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as SkeletonNode;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.LineColour = LineColour;
            typedTarget.SelectedBoneIndex = SelectedBoneIndex;
            typedTarget.SkeletonScale = SkeletonScale;
            typedTarget.Skeleton = Skeleton;
            base.CopyInto(target);
        }

        public void Dispose()
        {
            _lineRenderer.Dispose();
            _lineRenderer = null;
        }
    }
}
