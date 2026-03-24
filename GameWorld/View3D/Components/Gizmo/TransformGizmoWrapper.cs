using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Bone;
using GameWorld.Core.Commands.Vertex;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.Components.Gizmo
{
    public class TransformGizmoWrapper : ITransformable
    {
        /// <summary>
        /// Apply total rotation from original state to avoid accumulation error.
        /// Used during Immediate Transform mode for precise rotation.
        /// </summary>
        /// <param name="axisX">Screen X axis (horizontal on screen, points right)</param>
        /// <param name="axisY">Screen Y axis (vertical on screen, points up)</param>
        /// <param name="totalPitch">Total pitch angle in radians (rotation around X axis)</param>
        /// <param name="totalYaw">Total yaw angle in radians (rotation around Y axis)</param>
        public void ApplyTotalRotation(Vector3 axisX, Vector3 axisY, float totalPitch, float totalYaw)
        {
            if (_effectedObjects == null)
                return;

            // [FIX] Calculate total rotation matrix from accumulated angles
            // Pitch = rotation around screen X axis (horizontal, nodding motion)
            // Yaw = rotation around screen Y axis (vertical, shaking head motion)
            var pitchRotation = Matrix.CreateFromAxisAngle(axisX, totalPitch);
            var yawRotation = Matrix.CreateFromAxisAngle(axisY, totalYaw);
            var totalRotation = yawRotation * pitchRotation;

            // [FIX] Restore to original state first, then apply new total transform
            // This avoids floating point accumulation error
            var inverseOfPrevious = Matrix.Invert(_totalGizomTransform);
            var fullTransform = inverseOfPrevious * totalRotation;

            // Apply transform to all vertices
            foreach (var geo in _effectedObjects)
            {
                var objCenter = _originalPosition;

                if (_selectionState is ObjectSelectionState)
                {
                    for (var i = 0; i < geo.VertexCount(); i++)
                    {
                        var m = Matrix.CreateTranslation(-objCenter) * fullTransform * Matrix.CreateTranslation(objCenter);
                        geo.TransformVertex(i, m);
                    }
                }
                else if (_selectionState is VertexSelectionState vertSelectionState)
                {
                    for (var i = 0; i < vertSelectionState.VertexWeights.Count; i++)
                    {
                        if (vertSelectionState.VertexWeights[i] != 0)
                        {
                            var m = Matrix.CreateTranslation(-objCenter) * fullTransform * Matrix.CreateTranslation(objCenter);
                            geo.TransformVertex(i, m);
                        }
                    }
                }

                // [Performance] Rebuild vertex buffer once per mesh
                geo.RebuildVertexBuffer();
            }

            // Update accumulated transform
            _totalGizomTransform = totalRotation;
        }
        protected ILogger _logger = Logging.Create<TransformGizmoWrapper>();

        Vector3 _pos;
        public Vector3 Position { get => _pos; set { _pos = value; } }

        Vector3 _scale = Vector3.One;
        public Vector3 Scale { get => _scale; set { _scale = value; } }

        Quaternion _orientation = Quaternion.Identity;
        public Quaternion Orientation { get => _orientation; set { _orientation = value; } }

        ICommand _activeCommand;

        List<MeshObject> _effectedObjects;
        List<int> _selectedBones;
        private readonly CommandFactory _commandFactory;
        ISelectionState _selectionState;

        Matrix _totalGizomTransform = Matrix.Identity;
        bool _invertedWindingOrder = false;

        // [NEW] 保存原始位置用于取消恢复
        Vector3 _originalPosition;

        public TransformGizmoWrapper(CommandFactory commandFactory, List<MeshObject> effectedObjects, ISelectionState vertexSelectionState)
        {
            _commandFactory = commandFactory;
            _selectionState = vertexSelectionState;

            if (_selectionState as ObjectSelectionState != null)
            {
                _effectedObjects = effectedObjects;

                foreach (var item in _effectedObjects)
                    Position += item.MeshCenter;

                Position = Position / _effectedObjects.Count;
            }
            if (_selectionState is VertexSelectionState vertSelectionState)
            {
                _effectedObjects = effectedObjects;

                for (var i = 0; i < vertSelectionState.SelectedVertices.Count; i++)
                    Position += _effectedObjects[0].GetVertexById(vertSelectionState.SelectedVertices[i]);

                Position = Position / vertSelectionState.SelectedVertices.Count;
            }
        }

        public TransformGizmoWrapper(CommandFactory commandFactory, List<int> selectedBones, BoneSelectionState boneSelectionState)
        {
            _commandFactory = commandFactory;
            _selectionState = boneSelectionState;
            _selectedBones = selectedBones;

            _effectedObjects = new List<MeshObject> { boneSelectionState.RenderObject.Geometry };

            var sceneNode = boneSelectionState.RenderObject as Rmv2MeshNode;
            var animPlayer = sceneNode.AnimationPlayer;
            var currentFrame = animPlayer.GetCurrentAnimationFrame();
            var skeleton = boneSelectionState.Skeleton;

            if (currentFrame == null) return;

            var bones = boneSelectionState.SelectedBones;
            var totalBones = bones.Count;
            var rotations = new List<Quaternion>();
            foreach (var boneIdx in bones)
            {
                var bone = currentFrame.GetSkeletonAnimatedWorld(skeleton, boneIdx);
                bone.Decompose(out var scale, out var rot, out var trans);
                Position += trans;
                Scale += scale;
                rotations.Add(rot);

            }

            Orientation = AverageOrientation(rotations);
            Position = Position / totalBones;
            Scale = Scale / totalBones;
        }

        private Quaternion AverageOrientation(List<Quaternion> orientations)
        {
            var average = orientations[0];
            for (var i = 1; i < orientations.Count; i++)
            {
                average = Quaternion.Slerp(average, orientations[i], 1.0f / (i + 1));
            }
            return average;
        }

        // [NEW] 保存原始状态，在 Start 时调用
        public void SaveOriginalState()
        {
            _originalPosition = Position;
        }

        public void Cancel()
        {
            // [FIX] 应用逆变换恢复顶点
            if (_totalGizomTransform != Matrix.Identity && _effectedObjects != null)
            {
                // [性能优化] 只在有实际变换时才恢复
                var inverseTransform = Matrix.Invert(_totalGizomTransform);

                foreach (var geo in _effectedObjects)
                {
                    var objCenter = _originalPosition;

                    if (_selectionState is ObjectSelectionState)
                    {
                        for (var i = 0; i < geo.VertexCount(); i++)
                        {
                            var m = Matrix.CreateTranslation(-objCenter) * inverseTransform * Matrix.CreateTranslation(objCenter);
                            geo.TransformVertex(i, m);
                        }
                    }
                    else if (_selectionState is VertexSelectionState vertSelectionState)
                    {
                        for (var i = 0; i < vertSelectionState.VertexWeights.Count; i++)
                        {
                            if (vertSelectionState.VertexWeights[i] != 0)
                            {
                                var m = Matrix.CreateTranslation(-objCenter) * inverseTransform * Matrix.CreateTranslation(objCenter);
                                geo.TransformVertex(i, m);
                            }
                        }
                    }

                    geo.RebuildVertexBuffer();
                }
            }

            // [FIX] 重置位置到原始位置
            Position = _originalPosition;

            _activeCommand = null;
            _totalGizomTransform = Matrix.Identity;
        }

        public void Start(CommandExecutor commandManager)
        {

            if (_activeCommand is TransformVertexCommand transformVertexCommand)
            {
                transformVertexCommand.InvertWindingOrder = _invertedWindingOrder;
                transformVertexCommand.Transform = _totalGizomTransform;
                transformVertexCommand.PivotPoint = Position;
                commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
            }

            if (_activeCommand is TransformBoneCommand transformBoneCommand)
            {
                var matrix = _totalGizomTransform;
                matrix.Translation = Position;
                transformBoneCommand.Transform = matrix;
                commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
            }

            if (_selectionState is BoneSelectionState)
            {
                _totalGizomTransform = Matrix.Identity;
                _activeCommand = _commandFactory.Create<TransformBoneCommand>().Configure(x => x.Configure(_selectedBones, (BoneSelectionState)_selectionState)).Build();
            }
            else
            {
                _totalGizomTransform = Matrix.Identity;
                _activeCommand = _commandFactory.Create<TransformVertexCommand>().Configure(x => x.Configure(_effectedObjects, Position)).Build();
            }

        }

        public void Stop(CommandExecutor commandManager)
        {
            if (_activeCommand is TransformVertexCommand transformVertexCommand)
            {
                transformVertexCommand.InvertWindingOrder = _invertedWindingOrder;
                transformVertexCommand.Transform = _totalGizomTransform;
                transformVertexCommand.PivotPoint = Position;
                commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
                return;
            }

            if (_activeCommand is TransformBoneCommand transformBoneCommand)
            {
                var matrix = _totalGizomTransform;
                matrix.Translation = Position;
                transformBoneCommand.Transform = matrix;
                commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
                return;
            }
        }

        Matrix FixRotationAxis2(Matrix transform)
        {
            transform.Decompose(out var scale, out var rotation, out var translation);

            var flipQuaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.Pi);

            var correctedQuaternion = flipQuaternion * rotation;

            var fixedTransform = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(correctedQuaternion) * Matrix.CreateTranslation(translation);

            return fixedTransform;
        }


        public void GizmoTranslateEvent(Vector3 translation, PivotType pivot)
        {
            ApplyTransform(Matrix.CreateTranslation(translation), pivot, GizmoMode.Translate);
            Position += translation;
            _totalGizomTransform *= Matrix.CreateTranslation(translation);
        }

        public void GizmoRotateEvent(Matrix rotation, PivotType pivot)
        {
            ApplyTransform(rotation, pivot, GizmoMode.Rotate);
            _totalGizomTransform *= rotation;

            var fixedTransform = FixRotationAxis2(_totalGizomTransform);
            fixedTransform.Decompose(out var _, out var quat, out var _);
            Orientation = quat;
        }

        public void GizmoScaleEvent(Vector3 scale, PivotType pivot)
        {
            var realScale = scale + Vector3.One;
            var scaleMatrix = Matrix.CreateScale(scale + Vector3.One);
            ApplyTransform(scaleMatrix, pivot, GizmoMode.UniformScale);

            Scale += scale;

            _totalGizomTransform *= scaleMatrix;

            var negativeAxis = CountNegativeAxis(realScale);
            if (negativeAxis % 2 != 0)
            {
                _invertedWindingOrder = !_invertedWindingOrder;

                foreach (var geo in _effectedObjects)
                {
                    var indexes = geo.GetIndexBuffer();
                    for (var i = 0; i < indexes.Count; i += 3)
                    {
                        var temp = indexes[i + 2];
                        indexes[i + 2] = indexes[i + 0];
                        indexes[i + 0] = temp;
                    }
                    geo.SetIndexBuffer(indexes);
                }
            }
        }

        int CountNegativeAxis(Vector3 vector)
        {
            var result = 0;
            if (vector.X < 0) result++;
            if (vector.Y < 0) result++;
            if (vector.Z < 0) result++;
            return result;
        }

        void ApplyTransform(Matrix transform, PivotType pivotType, GizmoMode gizmoMode)
        {
            transform.Decompose(out var scale, out var rot, out var trans);

            if (_selectionState is BoneSelectionState boneSelectionState)
            {
                var objCenter = Vector3.Zero;
                if (pivotType == PivotType.ObjectCenter)
                    objCenter = Position;

                TransformBone(Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(rot) * Matrix.CreateTranslation(Position), objCenter, gizmoMode);
                return;
            }

            foreach (var geo in _effectedObjects)
            {
                var objCenter = Vector3.Zero;

                // [FIX] 对于旋转操作，使用原始位置作为中心，避免累积误差
                if (pivotType == PivotType.ObjectCenter)
                {
                    if (gizmoMode == GizmoMode.Rotate)
                        objCenter = _originalPosition;  // 使用原始位置
                    else
                        objCenter = Position;
                }

                if (_selectionState is ObjectSelectionState objectSelectionState)
                {
                    for (var i = 0; i < geo.VertexCount(); i++)
                        TransformVertex(transform, geo, objCenter, i);
                }
                else if (_selectionState is VertexSelectionState vertSelectionState)
                {
                    for (var i = 0; i < vertSelectionState.VertexWeights.Count; i++)
                    {
                        if (vertSelectionState.VertexWeights[i] != 0)
                        {
                            var weight = vertSelectionState.VertexWeights[i];
                            var vertexScale = Vector3.Lerp(Vector3.One, scale, weight);
                            var vertRot = Quaternion.Slerp(Quaternion.Identity, rot, weight);
                            var vertTrans = trans * weight;

                            var weightedTransform = Matrix.CreateScale(vertexScale) * Matrix.CreateFromQuaternion(vertRot) * Matrix.CreateTranslation(vertTrans);

                            TransformVertex(weightedTransform, geo, objCenter, i);
                        }
                    }
                }

                geo.RebuildVertexBuffer();
            }
        }
        void TransformBone(Matrix transform, Vector3 objCenter, GizmoMode gizmoMode)
        {
            if (_activeCommand is TransformBoneCommand transformBoneCommand)
            {
                transformBoneCommand.ApplyTransformation(transform, gizmoMode);
            }
        }

        void TransformVertex(Matrix transform, MeshObject geo, Vector3 objCenter, int index)
        {
            var m = Matrix.CreateTranslation(-objCenter) * transform * Matrix.CreateTranslation(objCenter);
            geo.TransformVertex(index, m);
        }

        public Vector3 GetObjectCentre()
        {
            return Position;
        }

        public static TransformGizmoWrapper CreateFromSelectionState(ISelectionState state, CommandFactory commandFactory)
        {
            if (state is ObjectSelectionState objectSelectionState)
            {
                var transformables = objectSelectionState.CurrentSelection().Where(x => x is ITransformable).Select(x => x.Geometry);
                if (transformables.Any())
                    return new TransformGizmoWrapper(commandFactory, transformables.ToList(), state);
            }
            else if (state is VertexSelectionState vertexSelectionState)
            {
                if (vertexSelectionState.SelectedVertices.Count != 0)
                    return new TransformGizmoWrapper(commandFactory, new List<MeshObject>() { vertexSelectionState.RenderObject.Geometry }, vertexSelectionState);
            }
            else if (state is BoneSelectionState boneSelectionState)
            {
                if (boneSelectionState.SelectedBones.Count != 0)
                    return new TransformGizmoWrapper(commandFactory, boneSelectionState.SelectedBones, boneSelectionState);
            }
            return null;
        }

    }
}
