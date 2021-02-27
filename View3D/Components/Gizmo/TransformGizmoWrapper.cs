using Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Vertex;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.Rendering.Geometry;

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

        TransformVertexCommand _activeCommand;

        List<IGeometry> _effectedObjects;
        List<int> _selectedVertexes;

        Matrix _totalGizomTransform = Matrix.Identity;


        public TransformGizmoWrapper(List<IGeometry> effectedObjects)
        {
            _effectedObjects = effectedObjects;

            foreach (var item in _effectedObjects)
                Position += item.MeshCenter;

            Position = (Position / _effectedObjects.Count);
        }

        public TransformGizmoWrapper(IGeometry vertexGeometry, List<int> selectedVertexes)
        {
            _effectedObjects = new List<IGeometry>() { vertexGeometry };
            _selectedVertexes = selectedVertexes;

            foreach (var item in _effectedObjects)
                Position += item.MeshCenter;

            Position = (Position / _effectedObjects.Count);
        }

        public void Start(GizmoMode mode)
        {
            _totalGizomTransform = Matrix.Identity;
            _activeCommand = new TransformVertexCommand(_effectedObjects, Position, mode == GizmoMode.Rotate, _selectedVertexes);
        }

        public void Stop(CommandExecutor commandManager)
        {
            if (_activeCommand != null)
            {
                _activeCommand.Transform = _totalGizomTransform;
                commandManager.ExecuteCommand(_activeCommand);
                _activeCommand = null;
            }
        }

        public void GizmoTranslateEvent(Vector3 translation, PivotType pivot)
        {
            ApplyTransform(Matrix.CreateTranslation(translation), pivot);
            Position += translation;
            _totalGizomTransform *= Matrix.CreateTranslation(translation);
        }

        public void GizmoRotateEvent(Matrix rotation, PivotType pivot)
        {
            ApplyTransform(rotation, pivot);// Rotate normal, bi normals and all that shit
            //Orientation += Quaternion.CreateFromRotationMatrix((Matrix)e.Value);  -> This enables the roation gizmo to update
            _totalGizomTransform *= rotation;
        }

        public void GizmoScaleEvent(Vector3 scale, PivotType pivot)
        {
            var scaleMatrix = Matrix.CreateScale(scale + Vector3.One);
            ApplyTransform(scaleMatrix, pivot);

            Scale += scale;
            _totalGizomTransform *= scaleMatrix;
        }

        void ApplyTransform(Matrix transform, PivotType pivotType)
        {
            foreach (var geo in _effectedObjects)
            {
                var objCenter = Vector3.Zero;
                if (pivotType == PivotType.ObjectCenter)
                    objCenter = Position;

                if (_selectedVertexes == null)
                {
                    for (int i = 0; i < geo.VertexCount(); i++)
                        TransformVertex(transform, geo, objCenter, i);
                }
                else
                {
                    for (int i = 0; i < _selectedVertexes.Count; i++)
                        TransformVertex(transform, geo, objCenter, _selectedVertexes[i]);
                }
               
                geo.RebuildVertexBuffer();
            }
        }

        void TransformVertex(Matrix transform, IGeometry geo, Vector3 objCenter, int index)
        {
            var vert = geo.GetVertexById(index);
            vert = vert - objCenter;
            vert = Vector3.Transform(vert, transform); // Rotate normal, bi normals and all that shit
            vert = vert + objCenter;
            geo.UpdateVertexPosition(index, vert);
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
                    return new TransformGizmoWrapper(transformables.ToList());
            }
            else if (state is VertexSelectionState vertexSelectionState)
            {
                if (vertexSelectionState.SelectedVertices.Count != 0)
                    return new  TransformGizmoWrapper(vertexSelectionState.RenderObject.Geometry, vertexSelectionState.SelectedVertices);
            }
            return null;
        }
       
    }
}
