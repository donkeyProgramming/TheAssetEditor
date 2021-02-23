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
    public class SkeletonNode : GroupNode, IDrawableItem
    {
        public GameSkeleton Skeleton { get; set; }

        LineMeshRender _lineRenderer;

        public Vector3 NodeColour = new Vector3(.25f, 1, .25f);
        public Vector3 SelectedNodeColour = new Vector3(1, 0, 0);
        public Vector3 LineColour = new Vector3(0, 0, 0);

        public int? SelectedBoneIndex { get; set; }
        public float SkeletonScale { get; set; } = 1;

        public SkeletonNode(ContentManager content, string name = "Skeleton") : base(name)
        {
            _lineRenderer = new LineMeshRender(content);
        }


        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (IsVisible && Skeleton != null)
            {
                Skeleton.Update();
                _lineRenderer.Clear();

                for (int i = 0; i < Skeleton.BoneCount; i++)
                {
                    var parentIndex = Skeleton.GetParentBone(i);
                    if (parentIndex == -1)
                        continue;

                    float scale = SkeletonScale;
                    Vector3 drawColour = NodeColour;
                    if (SelectedBoneIndex.HasValue && SelectedBoneIndex.Value == i)
                    {
                        drawColour = SelectedNodeColour;
                        scale *= 1.5f;
                    }

                    var boneMatrix = Skeleton.GetAnimatedWorldTranform(i);
                    var parentBoneMatrix = Skeleton.GetAnimatedWorldTranform(parentIndex);

                    _lineRenderer.AddCube(Matrix.CreateScale(scale) * Matrix.CreateScale(0.05f) * boneMatrix * parentWorld);
                    _lineRenderer.AddLine(Vector3.Transform(boneMatrix.Translation, parentWorld), Vector3.Transform(parentBoneMatrix.Translation, parentWorld));
                }

                renderEngine.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _lineRenderer, World = Matrix.Identity });
            }
        }

    }
}
