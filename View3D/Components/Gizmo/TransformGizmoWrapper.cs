using CommonControls.Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using View3D.Commands;
using View3D.Commands.Bone;
using View3D.Commands.Vertex;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;

namespace View3D.Components.Gizmo
{
    public class TransformGizmoWrapper : ITransformable
    {
        protected ILogger _logger = Logging.Create<TransformGizmoWrapper>();

        Vector3 _pos;
        public Vector3 Position { get=> _pos; set { _pos = value; } }

        Vector3 _scale = Vector3.One;
        public Vector3 Scale { get => _scale; set { _scale = value; } }

        Quaternion _orientation = Quaternion.Identity;
        public Quaternion Orientation { get => _orientation; set { _orientation = value; } }

        ICommand _activeCommand;

        List<MeshObject> _effectedObjects;
        ISelectionState _selectionState;

        Matrix _totalGizomTransform = Matrix.Identity;
        bool _invertedWindingOrder = false;

        public TransformGizmoWrapper(List<MeshObject> effectedObjects, ISelectionState vertexSelectionState)
        {
           
            _selectionState = vertexSelectionState;

            if (_selectionState as ObjectSelectionState != null)
            {
                _effectedObjects = effectedObjects;

                foreach (var item in _effectedObjects)
                    Position += item.MeshCenter;

                Position = (Position / _effectedObjects.Count);
            }
            if (_selectionState is VertexSelectionState vertSelectionState)
            {
                _effectedObjects = effectedObjects;

                for (int i = 0; i < vertSelectionState.SelectedVertices.Count; i++)
                    Position += _effectedObjects[0].GetVertexById(vertSelectionState.SelectedVertices[i]);

                Position = (Position / vertSelectionState.SelectedVertices.Count);
            }
        }


        public TransformGizmoWrapper(List<int> selectedBones, ISelectionState boneSelection)
        {
            _selectionState = boneSelection;

            if (_selectionState is BoneSelectionState boneSelectionState)
            {
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
                Position = (Position / totalBones);
                Scale = (Scale / totalBones);
            }

        }

        private Quaternion AverageOrientation(List<Quaternion> orientations) 
        {
            Quaternion average = orientations[0];
            for (int i = 1; i < orientations.Count; i++)
            {
                average = Quaternion.Slerp(average, orientations[i], 1.0f / (i + 1));
            }
            return average;
        }


        public void Start(CommandExecutor commandManager)
        {
            if (_activeCommand != null)
            {
                 //   MessageBox.Show("Transform debug check - Please inform the creator of the tool that you got this message. Would also love it if you tried undoing your last command to see if that works..\n E-001");
                 if(_activeCommand is TransformVertexCommand transformVertexCommand)
                {
                    transformVertexCommand.InvertWindingOrder = _invertedWindingOrder;
                    transformVertexCommand.Transform = _totalGizomTransform;
                    transformVertexCommand.PivotPoint = Position;
                    commandManager.ExecuteCommand(_activeCommand);
                    _activeCommand = null;
                }
                else if (_activeCommand is TransformBoneCommand transformBoneCommand)
                {
                    var matrix = _totalGizomTransform;
                    matrix.Translation = Position;
                    transformBoneCommand.Transform = matrix;
                    commandManager.ExecuteCommand(_activeCommand);
                    _activeCommand = null;
                }
            }

            _totalGizomTransform = Matrix.Identity;
            if(_selectionState is BoneSelectionState boneSelectionState)
            {
                _activeCommand = new TransformBoneCommand(boneSelectionState.SelectedBones, boneSelectionState);
            }
            else
            {
                _activeCommand = new TransformVertexCommand(_effectedObjects, Position);
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
            }
            else if (_activeCommand is TransformBoneCommand transformBoneCommand)
            {
                var matrix = _totalGizomTransform;
                matrix.Translation = Position;
                transformBoneCommand.Transform = matrix;
                commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
            }
        }

        Vector3 ToEulerAngles(Quaternion quaternion)
        {
            Vector3 eulerAngles;

            // Extract the pitch (x-axis rotation)
            float sinPitch = 2.0f * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            float cosPitch = 1.0f - 2.0f * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);
            eulerAngles.X = (float)Math.Atan2(sinPitch, cosPitch);

            // Extract the yaw (y-axis rotation)
            float sinYaw = 2.0f * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            if (Math.Abs(sinYaw) >= 1)
                eulerAngles.Y = (float)Math.CopySign(Math.PI / 2, sinYaw); // Use 90 degrees if out of range
            else
                eulerAngles.Y = (float)Math.Asin(sinYaw);

            // Extract the roll (z-axis rotation)
            float sinRoll = 2.0f * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            float cosRoll = 1.0f - 2.0f * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);
            eulerAngles.Z = (float)Math.Atan2(sinRoll, cosRoll);

            return eulerAngles;
        }

        Matrix FixRotationAxis(Matrix transform)
        {
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            transform.Decompose(out scale, out rotation, out translation);

            // Swap the X and Y rotations
            Vector3 euler = ToEulerAngles(rotation);
            rotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);

            // Recompose the transform with the fixed rotation
            Matrix fixedTransform = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);
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
            var fixedTransform = FixRotationAxis(_totalGizomTransform);
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
                    for (int i = 0; i < indexes.Count; i += 3)
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


            if(_selectionState is BoneSelectionState boneSelectionState)
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
                if (pivotType == PivotType.ObjectCenter)
                    objCenter = Position;

                if (_selectionState is ObjectSelectionState objectSelectionState)
                {
                    for (int i = 0; i < geo.VertexCount(); i++)
                        TransformVertex(transform, geo, objCenter, i);
                }
                else if(_selectionState is VertexSelectionState vertSelectionState)
                {
                    for (int i = 0; i < vertSelectionState.VertexWeights.Count; i++)
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
            if(_activeCommand is TransformBoneCommand transformBoneCommand)
            {
                var m = Matrix.CreateTranslation(-objCenter) * transform * Matrix.CreateTranslation(objCenter);
                transformBoneCommand.ApplyTransformation(m, gizmoMode);
            }
        }
        void TransformVertex(Matrix transform, MeshObject geo, Vector3 objCenter, int index)
        {
            var m = Matrix.CreateTranslation(-objCenter) * transform * Matrix.CreateTranslation(objCenter);
            geo.TransformVertex(index, m);
        }

        public Vector3 GetObjectCenter()
        {
            return Position;
        }

        public static TransformGizmoWrapper CreateFromSelectionState(ISelectionState state)
        {
            if (state is ObjectSelectionState objectSelectionState)
            {
                var transformables = objectSelectionState.CurrentSelection().Where(x => x is ITransformable).Select(x => x.Geometry);
                if (transformables.Any())
                    return new TransformGizmoWrapper(transformables.ToList(), state);
            }
            else if (state is VertexSelectionState vertexSelectionState)
            {
                if (vertexSelectionState.SelectedVertices.Count != 0)
                    return new  TransformGizmoWrapper(new List<MeshObject>(){vertexSelectionState.RenderObject.Geometry}, vertexSelectionState);
            }
            else if (state is BoneSelectionState boneSelectionState)
            {
                if (boneSelectionState.SelectedBones.Count != 0)
                    return new TransformGizmoWrapper(boneSelectionState.SelectedBones, boneSelectionState);
            }
            return null;
        }
       
    }
}
