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

        private IGizmoTransformable _activeTarget;
        private Matrix _initialMatrix;
        private Vector3 _trueWorldPivot;

        private Vector3 _lastWorldDeltaPos = Vector3.Zero;
        private Quaternion _lastWorldDeltaRot = Quaternion.Identity;

        public IGizmoTransformable ActiveTarget => _activeTarget;

        public bool HasValidTarget
        {
            get
            {
                if (_activeTarget != null) return true;
                if (_selectionState == null) return false;

                var mode = _selectionState.Mode;
                if (mode != GeometrySelectionMode.Object &&
                    mode != GeometrySelectionMode.Face &&
                    mode != GeometrySelectionMode.Vertex &&
                    mode != GeometrySelectionMode.Bone) return false;

                if (_selectionState is ObjectSelectionState objState) return objState.CurrentSelection().Any();
                if (_selectionState is VertexSelectionState vState) return vState.SelectedVertices.Count > 0;
                if (_selectionState is BoneSelectionState bState) return bState.SelectedBones.Count > 0;

                return false;
            }
        }

        public TransformGizmoWrapper(CommandFactory commandFactory, List<MeshObject> effectedObjects, ISelectionState vertexSelectionState)
        {
            _commandFactory = commandFactory;
            _selectionState = vertexSelectionState;

            if (_selectionState as ObjectSelectionState != null)
            {
                _effectedObjects = effectedObjects;
                foreach (var item in _effectedObjects) Position += item.MeshCenter;
                Position = Position / _effectedObjects.Count;
            }
            if (_selectionState is VertexSelectionState vertSelectionState)
            {
                _effectedObjects = effectedObjects;
                for (var i = 0; i < vertSelectionState.SelectedVertices.Count; i++) Position += _effectedObjects[0].GetVertexById(vertSelectionState.SelectedVertices[i]);
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
                average = Quaternion.Slerp(average, orientations[i], 1.0f / (i + 1));
            return average;
        }

        public void Start(CommandExecutor commandManager)
        {
            if (_activeTarget != null)
            {
                _totalGizomTransform = Matrix.Identity;
                StartDrag();
                return;
            }

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
            else if (_selectionState != null)
            {
                _totalGizomTransform = Matrix.Identity;
                _activeCommand = _commandFactory.Create<TransformVertexCommand>().Configure(x => x.Configure(_effectedObjects, Position)).Build();
            }
        }

        public void Stop(CommandExecutor commandManager)
        {
            if (_activeTarget != null)
            {
                EndDrag(commandManager);
                return;
            }

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
            if (_activeTarget != null)
            {
                Position += translation;
                _totalGizomTransform *= Matrix.CreateTranslation(translation);
                _totalGizomTransform.Decompose(out var _, out var totalDeltaRot, out var totalDeltaPos);
                ApplyTransformDelta(totalDeltaPos, totalDeltaRot, false);
                return;
            }

            // [FIX Bug 1] 原生工具的拖拽：正确累加总变换矩阵，这是保证最终撤销不弹回原点的核心！
            ApplyTransform(Matrix.CreateTranslation(translation), pivot, GizmoMode.Translate);
            Position += translation;
            _totalGizomTransform *= Matrix.CreateTranslation(translation);
        }

        public void GizmoRotateEvent(Matrix rotation, PivotType pivot)
        {
            if (_activeTarget != null)
            {
                _totalGizomTransform *= rotation;
                var fixedTransform = FixRotationAxis2(_totalGizomTransform);
                fixedTransform.Decompose(out var _, out var quat, out var _);
                Orientation = quat;

                _totalGizomTransform.Decompose(out var _, out var totalDeltaRot, out var totalDeltaPos);
                ApplyTransformDelta(totalDeltaPos, totalDeltaRot, false);
                return;
            }

            // [FIX Bug 1] 原生工具的拖拽：正确累加总变换矩阵
            ApplyTransform(rotation, pivot, GizmoMode.Rotate);
            _totalGizomTransform *= rotation;

            var fixedTransform2 = FixRotationAxis2(_totalGizomTransform);
            fixedTransform2.Decompose(out var _, out var quat2, out var _);
            Orientation = quat2;
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
            if (_activeTarget != null) return;

            transform.Decompose(out var scale, out var rot, out var trans);

            if (_selectionState is BoneSelectionState boneSelectionState)
            {
                var objCenter = Vector3.Zero;
                if (pivotType == PivotType.ObjectCenter) objCenter = Position;
                TransformBone(Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(rot) * Matrix.CreateTranslation(Position), objCenter, gizmoMode);
                return;
            }

            if (_effectedObjects == null) return;

            foreach (var geo in _effectedObjects)
            {
                var objCenter = Vector3.Zero;
                if (pivotType == PivotType.ObjectCenter) objCenter = Position;

                if (_selectionState is ObjectSelectionState objectSelectionState)
                {
                    for (var i = 0; i < geo.VertexCount(); i++) TransformVertex(transform, geo, objCenter, i);
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
                            var vertTrnas = trans * weight;
                            var weightedTransform = Matrix.CreateScale(vertexScale) * Matrix.CreateFromQuaternion(vertRot) * Matrix.CreateTranslation(vertTrnas);
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
                transformBoneCommand.ApplyTransformation(transform, gizmoMode);
        }

        void TransformVertex(Matrix transform, MeshObject geo, Vector3 objCenter, int index)
        {
            var m = Matrix.CreateTranslation(-objCenter) * transform * Matrix.CreateTranslation(objCenter);
            geo.TransformVertex(index, m);
        }

        public Vector3 GetObjectCentre() => Position;

        public static TransformGizmoWrapper CreateFromSelectionState(ISelectionState state, CommandFactory commandFactory)
        {
            if (state is ObjectSelectionState objectSelectionState)
            {
                var transformables = objectSelectionState.CurrentSelection().Where(x => x is ITransformable).Select(x => x.Geometry);
                if (transformables.Any()) return new TransformGizmoWrapper(commandFactory, transformables.ToList(), state);
            }
            else if (state is VertexSelectionState vertexSelectionState)
            {
                if (vertexSelectionState.SelectedVertices.Count != 0) return new TransformGizmoWrapper(commandFactory, new List<MeshObject>() { vertexSelectionState.RenderObject.Geometry }, vertexSelectionState);
            }
            else if (state is BoneSelectionState boneSelectionState)
            {
                if (boneSelectionState.SelectedBones.Count != 0) return new TransformGizmoWrapper(commandFactory, boneSelectionState.SelectedBones, boneSelectionState);
            }
            return null;
        }

        public static TransformGizmoWrapper CreateFromGizmoTransformable(IGizmoTransformable target, CommandFactory commandFactory)
        {
            var wrapper = new TransformGizmoWrapper(commandFactory, new List<MeshObject>(), null);
            wrapper.SetTarget(target);
            return wrapper;
        }

        public void SetTarget(IGizmoTransformable target)
        {
            _activeTarget = target;
            if (_activeTarget != null)
            {
                Position = _activeTarget.Pivot;
                _activeTarget.WorldMatrix.Decompose(out var scale, out var rot, out var trans);
                Orientation = rot;
                Scale = scale;
            }
        }

        public void StartDrag()
        {
            _lastWorldDeltaPos = Vector3.Zero;
            _lastWorldDeltaRot = Quaternion.Identity;

            if (_activeTarget == null) return;

            _initialMatrix = _activeTarget.WorldMatrix;
            _trueWorldPivot = _activeTarget.Pivot;

            _activeTarget.OnGizmoDragStart();
        }

        public void EndDrag(CommandExecutor commandManager)
        {
            _activeTarget?.OnGizmoDragEnd(commandManager);
        }

        public void ApplyTransformDelta(Vector3 worldDeltaPos, Quaternion worldDeltaRot, bool syncGizmoVisuals = true)
        {
            if (_activeTarget != null)
            {
                var newMatrix = SolveInPlaceMatrix(_initialMatrix, _trueWorldPivot, worldDeltaRot, worldDeltaPos);
                _activeTarget.WorldMatrix = newMatrix;

                if (syncGizmoVisuals)
                {
                    Position = _activeTarget.Pivot;
                    newMatrix.Decompose(out var scale, out var rot, out var trans);
                    Orientation = rot;
                    Scale = scale;
                }
            }
            else
            {
                // [FIX Bug 1] 回退到了绝对稳固的 Kitbash 增量分解解算逻辑，保证每帧都能转化为最纯净的矩阵相乘。
                Vector3 framePosDelta = worldDeltaPos - _lastWorldDeltaPos;
                Quaternion frameRotDelta = worldDeltaRot * Quaternion.Inverse(_lastWorldDeltaRot);

                _lastWorldDeltaPos = worldDeltaPos;
                _lastWorldDeltaRot = worldDeltaRot;

                if (framePosDelta != Vector3.Zero)
                {
                    ApplyTransform(Matrix.CreateTranslation(framePosDelta), PivotType.ObjectCenter, GizmoMode.Translate);
                    Position += framePosDelta;
                    _totalGizomTransform *= Matrix.CreateTranslation(framePosDelta);
                }

                if (frameRotDelta != Quaternion.Identity)
                {
                    var rotMatrix = Matrix.CreateFromQuaternion(frameRotDelta);
                    ApplyTransform(rotMatrix, PivotType.ObjectCenter, GizmoMode.Rotate);
                    _totalGizomTransform *= rotMatrix;

                    var fixedTransform = FixRotationAxis2(_totalGizomTransform);
                    fixedTransform.Decompose(out var _, out var quat, out var _);
                    Orientation = quat;
                }
            }
        }

        public Matrix SolveInPlaceMatrix(Matrix initialMatrix, Vector3 trueWorldPivot, Quaternion worldDeltaRot, Vector3 worldDeltaPos)
        {
            return initialMatrix
                   * Matrix.CreateTranslation(-trueWorldPivot)
                   * Matrix.CreateFromQuaternion(worldDeltaRot)
                   * Matrix.CreateTranslation(trueWorldPivot)
                   * Matrix.CreateTranslation(worldDeltaPos);
        }
    }
}
