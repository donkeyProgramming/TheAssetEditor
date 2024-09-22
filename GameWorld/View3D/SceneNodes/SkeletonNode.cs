using GameWorld.Core.Animation;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.SceneNodes
{
    public interface ISkeletonProvider
    {
        GameSkeleton Skeleton { get; }
    }

    public class SkeletonNode : GroupNode, IDrawableItem
    {
        public Color NodeColour { get; set; } = Color.Black;
        public Color SelectedNodeColour { get; set; } = Color.Red;
        public Vector3 LineColour { get; set; } = new Vector3(0, 0, 0);

        public int? SelectedBoneIndex { get; set; }
        public float SkeletonScale { get; set; } = 1;
        public float SelectedBoneScaleMult { get; set; } = 1.5f;

        public GameSkeleton? Skeleton { get; set; }

        public SkeletonNode(GameSkeleton? skeleton, string name = "Skeleton") : base(name)
        {
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
                for (var i = 0; i < Skeleton.BoneCount; i++)
                {
                    var scale = SkeletonScale;
                    var drawColour = NodeColour;
                    if (SelectedBoneIndex.HasValue && SelectedBoneIndex.Value == i)
                    {
                        drawColour = SelectedNodeColour;
                        scale *= SelectedBoneScaleMult;
                    }

                    var boneMatrix = Skeleton.GetAnimatedWorldTranform(i);
                    renderEngine.AddRenderLines(LineHelper.CreateCube(Matrix.CreateScale(scale) * Matrix.CreateScale(0.05f) * boneMatrix * Matrix.CreateScale(ScaleMult) * parentWorld, drawColour));

                    var parentIndex = Skeleton.GetParentBoneIndex(i);
                    if (parentIndex != -1)
                    {
                        var currentBoneMatrix = boneMatrix * Matrix.CreateScale(ScaleMult);
                        var parentBoneMatrix = Skeleton.GetAnimatedWorldTranform(parentIndex) * Matrix.CreateScale(ScaleMult);
                        renderEngine.AddRenderLines(LineHelper.AddLine(Vector3.Transform(currentBoneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld), Color.Black));
                    }
                }
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
    }
}
