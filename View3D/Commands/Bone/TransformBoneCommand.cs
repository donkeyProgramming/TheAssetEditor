using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Animation;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Commands.Bone
{
    public class TransformBoneCommand : CommandBase<TransformBoneCommand>
    {
        List<int> _selectedBones;
        BoneSelectionState _boneSelectionState;
        int _currentFrame;
        AnimationClip.KeyFrame _oldFrame;

        public Vector3 PivotPoint;
        public Matrix Transform { get; set; }

        SelectionManager _selectionManager;
        ISelectionState _oldSelectionState;


        public TransformBoneCommand(List<int> selectedBones, Vector3 pivotPoint, BoneSelectionState state)
        {
            _selectedBones = selectedBones;
            _boneSelectionState = state;
            _currentFrame = state.CurrentFrame;
            _oldFrame = state.CurrentAnimation.DynamicFrames[_currentFrame].Copy();

            PivotPoint = pivotPoint;
        }

        public override string GetHintText()
        {
            return "Transform Bone";
        }

        public override void Initialize(IComponentManager componentManager)
        {
            _selectionManager = componentManager.GetComponent<SelectionManager>();
        }

        protected override void ExecuteCommand()
        {
            _oldSelectionState = _selectionManager.GetStateCopy();
            Console.WriteLine($" gizmo moved: {Transform.Translation}");
            Console.WriteLine($" gizmo pivotPoint: {PivotPoint}");
            var selectedBone = _selectedBones[0];
            var node = _boneSelectionState.RenderObject as Rmv2MeshNode;
            var animationPlayer = node.AnimationPlayer;
            var currentAnimFrame = animationPlayer.GetCurrentAnimationFrame();
            var currentLocalBoneTransform = currentAnimFrame.GetLocalTransform(selectedBone);
            var currentBoneWorldTransform = currentAnimFrame.GetSkeletonAnimatedWorld(_boneSelectionState.Skeleton, selectedBone);
            Console.WriteLine($"current bone pos: {currentBoneWorldTransform.Translation}");
            Console.WriteLine($"current bone local pos: {currentLocalBoneTransform.Translation}");
            currentBoneWorldTransform.Translation = currentBoneWorldTransform.Translation + Transform.Translation;
            var newBoneTransform = currentAnimFrame.GetSkeletonAnimatedBoneFromWorld(_boneSelectionState.Skeleton, selectedBone, currentBoneWorldTransform );
            Console.WriteLine($"new bone local pos: {newBoneTransform.Translation}");
            Console.WriteLine($"new bone pos: {currentBoneWorldTransform.Translation}");

            //convert bone from world pos to bone pos
            //manipulate the bones

        }

        protected override void UndoCommand()
        {
            //restore from _oldFrame
        }

    }
}
