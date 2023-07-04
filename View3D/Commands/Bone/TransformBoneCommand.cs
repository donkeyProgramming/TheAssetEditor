using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;
using View3D.SceneNodes;

namespace View3D.Commands.Bone
{
    public class TransformBoneCommand : ICommand
    {
        List<int> _selectedBones;
        BoneSelectionState _boneSelectionState;
        int _currentFrame;
        AnimationClip.KeyFrame _oldFrame;
        public Matrix Transform { get; set; }

        public string HintText => "Bone Transform";

        public bool IsMutation => true;

        private Matrix _oldTransform = Matrix.Identity;

        ISelectionState _oldSelectionState;


        public TransformBoneCommand(List<int> selectedBones, BoneSelectionState state)
        {
            _selectedBones = selectedBones;
            _boneSelectionState = state;
            _currentFrame = state.CurrentFrame;
            _oldFrame = state.CurrentAnimation.DynamicFrames[_currentFrame].Copy();
        }

        public void ApplyTransformation(Matrix newPosition, GizmoMode gizmoMode)
        {
            if(_oldTransform ==  Matrix.Identity)
            {
                _oldTransform = newPosition;
                return;
            }

            var matrixDelta = newPosition - _oldTransform;
            _oldTransform = newPosition;
            Console.WriteLine($" gizmo moved: {Transform.Translation}");
            //Console.WriteLine($" gizmo pivotPoint: {PivotPoint}");
            foreach (var selectedBone in _selectedBones)
            {
                var node = _boneSelectionState.RenderObject as Rmv2MeshNode;
                var animationPlayer = node.AnimationPlayer;
                var currentAnimFrame = animationPlayer.GetCurrentAnimationFrame();
                var currentBoneWorldTransform = currentAnimFrame.GetSkeletonAnimatedWorld(_boneSelectionState.Skeleton, selectedBone);
                currentBoneWorldTransform.Translation += matrixDelta.Translation;
                var newBoneTransform = currentAnimFrame.GetSkeletonAnimatedBoneFromWorld(_boneSelectionState.Skeleton, selectedBone, currentBoneWorldTransform);

                Console.WriteLine(_boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Position[selectedBone]);
                newBoneTransform.Decompose(out var scale, out var rot, out var trans);
                newPosition.Decompose(out var newScale, out var rot2, out var trans2);
                switch (gizmoMode)
                {
                    case GizmoMode.Translate:
                        _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Position[selectedBone] += trans;
                        break;
                    case GizmoMode.Rotate:
                        _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Rotation[selectedBone] *= rot2;
                        break;
                    case GizmoMode.NonUniformScale:
                    case GizmoMode.UniformScale:
                        _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Scale[selectedBone] = scale;
                        break;
                    default:
                        throw new InvalidOperationException("unknown gizmo mode");
                }
            }
        }

        public void Undo()
        {
            var selectionState = _oldSelectionState as BoneSelectionState;
            selectionState.CurrentAnimation.DynamicFrames[_currentFrame] = _oldFrame;
        }

        public void Execute()
        {
            _oldSelectionState = _boneSelectionState;
            _oldFrame = _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Copy();
        }
    }
}
