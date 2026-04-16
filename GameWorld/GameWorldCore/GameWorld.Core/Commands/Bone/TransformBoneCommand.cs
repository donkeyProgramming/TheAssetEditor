using System;
using System.Collections.Generic;
using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using static GameWorld.Core.Animation.AnimationClip;

namespace GameWorld.Core.Commands.Bone
{
    public class TransformBoneCommand : ICommand
    {
        List<int> _selectedBones;
        BoneSelectionState _boneSelectionState;
        int _currentFrame;
        KeyFrame _oldFrame;
        public Matrix Transform { get; set; }

        public string HintText => "Bone Transform";

        public bool IsMutation => true;

        private Matrix _oldTransform = Matrix.Identity;

        ISelectionState _oldSelectionState;
        private SelectionManager _selectionManager;

        public TransformBoneCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<int> selectedBones, BoneSelectionState state)
        {
            _selectedBones = selectedBones;
            _boneSelectionState = state;
            _currentFrame = state.CurrentFrame;
            _oldFrame = _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Clone();
        }

        public void ApplyTransformation(Matrix newPosition, GizmoMode gizmoMode)
        {
            if (_selectedBones.Count == 0) return;

            //TODO: FIX ME
            //if(_boneSelectionState.EnableInverseKinematics)
            //{
            //    ApplyTransformationInverseKinematic(newPosition, _selectedBones[0], _boneSelectionState.InverseKinematicsEndBoneIndex);
            //    return;
            //}
            if (_oldTransform == Matrix.Identity)
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
                var newBoneTransform = GetSkeletonAnimatedBoneFromWorld(currentAnimFrame, _boneSelectionState.Skeleton, selectedBone, currentBoneWorldTransform);

                Console.WriteLine(_boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Position[selectedBone]);
                newBoneTransform.Decompose(out var scale, out var rot, out var trans);
                newPosition.Decompose(out var newScale, out var rot2, out var trans2);
                var modifiedTransform = _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Clone();
                switch (gizmoMode)
                {
                    case GizmoMode.Translate:
                        modifiedTransform.Position[selectedBone] += trans;
                        break;
                    case GizmoMode.Rotate:
                        modifiedTransform.Rotation[selectedBone] *= rot2;
                        break;
                    case GizmoMode.NonUniformScale:
                    case GizmoMode.UniformScale:
                        modifiedTransform.Scale[selectedBone] = scale;
                        break;
                    default:
                        throw new InvalidOperationException("unknown gizmo mode");
                }

                _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame] = modifiedTransform;
            }

            _boneSelectionState.TriggerModifiedBoneEvent(_selectedBones);
        }

        public Matrix GetSkeletonAnimatedBoneFromWorld(AnimationFrame frame, GameSkeleton gameSkeleton, int boneIndex, Matrix objectInWorldTransform)
        {
            var output = objectInWorldTransform * Matrix.Invert(frame.GetSkeletonAnimatedWorld(gameSkeleton, boneIndex));
            return output;
        }

        //TODO: FIX ME
        void ApplyTransformationInverseKinematic(Matrix newPosition, int startBone, int endBone)
        {
            var node = _boneSelectionState.RenderObject as Rmv2MeshNode;
            var animationPlayer = node.AnimationPlayer;
            var currentAnimFrame = animationPlayer.GetCurrentAnimationFrame();

            // Get the chain of bones from startBone to endBone
            var boneCount = 1;
            var boneIndex = startBone;
            while (boneIndex != endBone)
            {
                boneIndex = currentAnimFrame.GetParentBoneIndex(_boneSelectionState.Skeleton, boneIndex);
                boneCount++;
            }
            boneIndex = startBone;
            var positions = new Vector3[boneCount];
            var rotations = new Quaternion[boneCount];
            var boneLengths = new float[boneCount - 1];
            var boneIndices = new int[boneCount];
            float totalLength = 0;


            for (var i = 0; i < boneCount; i++)
            {
                var transform = Matrix.CreateScale(1);
                // Get the current bone world transform
                var currentBoneWorldTransform = currentAnimFrame.GetSkeletonAnimatedWorld(_boneSelectionState.Skeleton, boneIndex);

                // Store the position and rotation of the current bone
                positions[i] = currentBoneWorldTransform.Translation;
                currentBoneWorldTransform.Decompose(out _, out var rotation, out _);
                rotations[i] = rotation;
                boneIndices[i] = boneIndex;

                // Calculate the length of the bone and add it to the total length
                if (i < boneCount - 1)
                {
                    boneLengths[i] = Vector3.Distance(positions[i], positions[i + 1]);
                    totalLength += boneLengths[i];
                }

                // Move to the next bone in the chain
                boneIndex = currentAnimFrame.GetParentBoneIndex(_boneSelectionState.Skeleton, boneIndex);
            }

            // Check if the target is reachable
            if (Vector3.Distance(newPosition.Translation, positions[0]) > totalLength)
            {
                // The target is unreachable, move the end effector towards the target
                for (var i = boneCount - 2; i >= 0; i--)
                {
                    positions[i + 1] = newPosition.Translation;
                    var direction = Vector3.Normalize(positions[i] - positions[i + 1]);
                    positions[i] = positions[i + 1] + direction * boneLengths[i];
                }
            }
            else
            {
                // The target is reachable, apply the FABRIK algorithm
                var rootPosition = positions[0];
                var tolerance = 0.01f;
                while (Vector3.Distance(newPosition.Translation, positions[boneCount - 1]) > tolerance)
                {
                    // Stage 1: Forward reaching
                    ForwardReaching(positions, boneLengths, newPosition.Translation, boneCount - 2);

                    // Stage 2: Backward reaching
                    BackwardReaching(positions, boneLengths, rootPosition, 0);
                }

                // Update the position and rotation of each bone in the chain
                for (var i = 0; i < boneCount - 1; i++)
                {
                    _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Position[boneIndices[i]] = positions[i];
                    continue;
                    //if (i < boneCount - 1)
                    //{
                    //    var direction = Vector3.Normalize(positions[i + 1] - positions[i]);
                    //    var rotation = Quaternion.CreateFromAxisAngle(Vector3.Cross(Vector3.UnitX, direction), (float)Math.Acos(Vector3.Dot(Vector3.UnitX, direction)));
                    //    _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame].Rotation[boneIndices[i]] = rotation;
                    //}
                }
            }

        }

        //TODO: FIX ME
        private void ForwardReaching(Vector3[] positions, float[] boneLengths, Vector3 targetPosition, int index)
        {
            if (index < 0) return;

            positions[index + 1] = targetPosition;
            var direction = Vector3.Normalize(positions[index] - positions[index + 1]);
            positions[index] = positions[index + 1] + direction * boneLengths[index];

            ForwardReaching(positions, boneLengths, positions[index], index - 1);
        }

        //TODO: FIX ME
        private void BackwardReaching(Vector3[] positions, float[] boneLengths, Vector3 rootPosition, int index)
        {
            if (index >= positions.Length - 1) return;

            positions[index] = rootPosition;
            var direction = Vector3.Normalize(positions[index + 1] - positions[index]);
            positions[index + 1] = positions[index] + direction * boneLengths[index];

            BackwardReaching(positions, boneLengths, positions[index + 1], index + 1);
        }

        public static void CompareKeyFrames(KeyFrame A, KeyFrame B)
        {
            for (var j = 0; j < A.Position.Count; j++)
            {
                var posDiff = A.Position[j] - B.Position[j];
                var rotDiff = A.Rotation[j].ToVector4() - B.Rotation[j].ToVector4();
                var scaleDiff = A.Scale[j] - B.Scale[j];
                if (posDiff != new Vector3(0) || rotDiff != new Vector4(0) || scaleDiff != new Vector3(0))
                    Console.WriteLine($"Bone {j}: Position difference: {posDiff}, Rotation difference: {rotDiff}, Scale difference: {scaleDiff}");
            }
        }

        public void Undo()
        {
            if (_oldFrame == null) return;
            CompareKeyFrames(_oldFrame, _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame]);
            _boneSelectionState.CurrentAnimation.DynamicFrames[_currentFrame] = _oldFrame.Clone();
            _boneSelectionState.TriggerModifiedBoneEvent(_selectedBones);
        }

        public void Execute()
        {
            _oldSelectionState = _boneSelectionState;
        }
    }
}
