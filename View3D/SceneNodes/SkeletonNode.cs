using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public interface ISkeletonProvider
    { 
        bool IsActive { get; }
        GameSkeleton Skeleton { get;  }
    }

    public class SimpleSkeletonProvider : ISkeletonProvider
    {
        public SimpleSkeletonProvider(GameSkeleton skeleton) { Skeleton = skeleton; }
        public bool IsActive => true;

        public GameSkeleton Skeleton { get; private set; }
    }


    public class SkeletonNode : GroupNode, IDrawableItem, IDisposable
    {
        public ISkeletonProvider AnimationProvider { get; private set; }
        LineMeshRender _lineRenderer;

        public Color NodeColour = Color.Black;
        public Color SelectedNodeColour = Color.Red;
        public Vector3 LineColour = new Vector3(0, 0, 0);

        public int? SelectedBoneIndex { get; set; }
        public float SkeletonScale { get; set; } = 1;

        public SkeletonNode(IComponentManager componentManager, ISkeletonProvider animationProvider, string name = "Skeleton") : base(name)
        {
            _lineRenderer = new LineMeshRender(componentManager.GetComponent<ResourceLibary>());
            AnimationProvider = animationProvider;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var skeleton = AnimationProvider.Skeleton;
           
            if (skeleton != null)
                Name = AnimationProvider.Skeleton.SkeletonName;
            else
                Name = "Skeleton ";

            if (IsVisible && skeleton != null/* && _animationProvider.IsActive*/)
            {
                _lineRenderer.Clear();

                for (int i = 0; i < skeleton.BoneCount; i++)
                {
                    var parentIndex = skeleton.GetParentBone(i);
                    if (parentIndex == -1)
                    {
                        var boneMatrix2 = skeleton.GetAnimatedWorldTranform(i);
                        _lineRenderer.AddCube(Matrix.CreateScale(SkeletonScale) * Matrix.CreateScale(0.05f) * boneMatrix2 * parentWorld, NodeColour);
                        continue;
                    }
                        

                    float scale = SkeletonScale;
                    Color drawColour = NodeColour;
                    if (SelectedBoneIndex.HasValue && SelectedBoneIndex.Value == i)
                    {
                        drawColour = SelectedNodeColour;
                        scale *= 1.5f;
                    }

                    var boneMatrix = skeleton.GetAnimatedWorldTranform(i);
                    var parentBoneMatrix = skeleton.GetAnimatedWorldTranform(parentIndex);

                    _lineRenderer.AddCube(Matrix.CreateScale(scale) * Matrix.CreateScale(0.05f) * boneMatrix * parentWorld, drawColour);
                    _lineRenderer.AddLine(Vector3.Transform(boneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld));
                }

                renderEngine.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _lineRenderer, ModelMatrix = Matrix.Identity });
            }
        }


        protected SkeletonNode() { }

        public override ISceneNode CreateCopyInstance() => new SkeletonNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as SkeletonNode;
            typedTarget.NodeColour = NodeColour;
            typedTarget.SelectedNodeColour = SelectedNodeColour;
            typedTarget.LineColour = LineColour;
            typedTarget.SelectedBoneIndex = SelectedBoneIndex;
            typedTarget.SkeletonScale = SkeletonScale;
            base.CopyInto(target);
        }

        public void Dispose()
        {
            _lineRenderer.Dispose();
            _lineRenderer = null;
        }
    }
}
