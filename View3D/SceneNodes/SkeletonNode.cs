using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;

namespace View3D.SceneNodes
{
    public interface IAnimationProvider
    { 
        bool IsActive { get; }
        GameSkeleton Skeleton { get; set; }
    }

    public class SkeletonNode : GroupNode, IDrawableItem, IDisposable
    {
        public IAnimationProvider AnimationProvider { get; private set; }
        LineMeshRender _lineRenderer;

        public Vector3 NodeColour = new Vector3(.25f, 1, .25f);
        public Vector3 SelectedNodeColour = new Vector3(1, 0, 0);
        public Vector3 LineColour = new Vector3(0, 0, 0);

        public int? SelectedBoneIndex { get; set; }
        public float SkeletonScale { get; set; } = 1;

        public SkeletonNode(ContentManager content, IAnimationProvider animationProvider, string name = "Skeleton") : base(name)
        {
            _lineRenderer = new LineMeshRender(content);
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
                //skeleton.Update();
                _lineRenderer.Clear();

                for (int i = 0; i < skeleton.BoneCount; i++)
                {
                    var parentIndex = skeleton.GetParentBone(i);
                    if (parentIndex == -1)
                        continue;

                    float scale = SkeletonScale;
                    Vector3 drawColour = NodeColour;
                    if (SelectedBoneIndex.HasValue && SelectedBoneIndex.Value == i)
                    {
                        drawColour = SelectedNodeColour;
                        scale *= 1.5f;
                    }

                    var boneMatrix = skeleton.GetAnimatedWorldTranform(i);
                    var parentBoneMatrix = skeleton.GetAnimatedWorldTranform(parentIndex);

                    _lineRenderer.AddCube(Matrix.CreateScale(scale) * Matrix.CreateScale(0.05f) * boneMatrix * parentWorld);
                    _lineRenderer.AddLine(Vector3.Transform(boneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld));
                }

                renderEngine.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _lineRenderer, World = Matrix.Identity });
            }
        }

        public void Dispose()
        {
            _lineRenderer.Dispose();
            _lineRenderer = null;
        }
    }
}
