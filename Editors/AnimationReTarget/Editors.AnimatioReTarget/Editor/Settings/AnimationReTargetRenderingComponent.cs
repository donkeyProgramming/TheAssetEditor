using Editors.Shared.Core.Common;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace Editors.AnimatioReTarget.Editor.Settings
{
    public class AnimationReTargetRenderingComponent : BaseComponent
    {
        record RenderInfo(string Name, float Radius);

        private readonly RenderEngineComponent _renderEngineComponent;
        float _radius = 1;
        float _visualOffset = 2;
        SceneObject? _source;
        SceneObject? _target; 
        SceneObject? _generated;

     
        public float VisualOffset
        {
            get => _visualOffset;
            set => SetAndNotify(ref _visualOffset, value);  
        }

        public bool ShowGeneratedMesh
        {
            get
            {
                if (_generated == null)
                    return true;
                return _generated.ShowMesh.Value;
            }
            set
            {
                if (_generated == null)
                    return;
                _generated.ShowMesh.Value = value;
            }
        }

        public bool ShowGeneratedSkeleton
        {
            get
            {
                if (_generated == null)
                    return true;
                return _generated.ShowSkeleton.Value;
            }
            set
            {
                if (_generated == null)
                    return;
                _generated.ShowSkeleton.Value = value;
            }
        }


        public AnimationReTargetRenderingComponent(RenderEngineComponent renderEngineComponent)
        {
            _renderEngineComponent = renderEngineComponent;
        }

        public void ComputeOffsets()
        {
            var r0 = ComputeWidth(_source);
            var r1 = ComputeWidth(_target);
            var r2 = ComputeWidth(_generated);

            _radius = Math.Max(r0, Math.Max(r1,r2));
        }

        public override void Draw(GameTime gameTime) 
        {
            var p0 = new Vector3(-_radius * _visualOffset, 0, 0);
            var p1 = new Vector3(0 * _visualOffset, 0, 0);
            var p2 = new Vector3(_radius * _visualOffset, 0, 0);

            if (_source != null)
                _source.Offset = Matrix.CreateTranslation(p0);
            if (_generated != null)
                _generated.Offset = Matrix.CreateTranslation(p1);
            if (_target != null)
                _target.Offset = Matrix.CreateTranslation(p2);

            _renderEngineComponent.AddRenderLines(LineHelper.AddCircle(p0, _radius, Color.Red));
            _renderEngineComponent.AddRenderLines(LineHelper.AddCircle(p1, _radius, Color.Green));
            _renderEngineComponent.AddRenderLines(LineHelper.AddCircle(p2, _radius, Color.Blue));

            var p0Text = p0 + new Vector3(0, 0, _radius);
            var p1Text = p1 + new Vector3(0, 0, _radius);
            var p2Text = p2 + new Vector3(0, 0, _radius);
            var renderText0 = new WorldTextRenderItem(_renderEngineComponent, "Source", p0Text);
            var renderText1 = new WorldTextRenderItem(_renderEngineComponent, "Generated", p1Text);
            var renderText2 = new WorldTextRenderItem(_renderEngineComponent, "Target", p2Text);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, renderText0);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, renderText1);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, renderText2);
        }

        public void SetSceneNodes(SceneObject source, SceneObject target, SceneObject generated)
        {
            _source = source;
            _target = target;
            _generated = generated;
            ComputeOffsets();
        }

        float ComputeWidth(SceneObject? sceneObject)
        {
            float defaultValue = 0.1f;
            if (sceneObject == null)
                return defaultValue;
       
            var modelNodes = SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(sceneObject.MainNode);

            var bb = new BoundingSphere(Vector3.Zero, defaultValue);
            foreach (var modelNode in modelNodes)
            {
                var meshes = modelNode.GetMeshesInLod(0, false);
                foreach (var mesh in meshes)
                {

                    var meshBB = BoundingSphere.CreateFromBoundingBox(mesh.Geometry.BoundingBox);
                    bb = BoundingSphere.CreateMerged(bb, meshBB);
                }
            }
            
            return bb.Radius;
        }
    }

}
