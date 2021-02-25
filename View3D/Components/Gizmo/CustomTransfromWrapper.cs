using Common;
using Microsoft.Xna.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Vertex;
using View3D.Components.Component;
using View3D.Rendering.Geometry;

namespace View3D.Components.Gizmo
{
    public class CustomTransfromWrapper : ITransformable
    {
        protected ILogger _logger = Logging.Create<CustomTransfromWrapper>();

        Vector3 _pos;
        public Vector3 Position { get=> _pos; set { _pos = value; } }

        Vector3 _scale = Vector3.One;
        public Vector3 Scale { get => _scale; set { _scale = value; } }

        Quaternion _orientation = Quaternion.Identity;
        public Quaternion Orientation { get => _orientation; set { _orientation = value; } }

        public Matrix ModelMatrix { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGeometry Geometry { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IGeometry _geo;
        List<int> _vertexList;


        TransformVertexCommand2 _activeCommand;

        List<ITransformable> _effectedObjects;
        Matrix _totalGizomTransform = Matrix.Identity;

       

        public CustomTransfromWrapper(List<ITransformable> effectedObjects)
        {
            _effectedObjects = effectedObjects;

            foreach (var item in _effectedObjects)
            {
                var geo = item.Geometry;
                var objCenter = Vector3.Zero;

                for (int i = 0; i < geo.VertexCount(); i++)
                    objCenter += geo.GetVertexById(i);

                objCenter = objCenter / geo.VertexCount();
                Position += objCenter;
            }

            Position =  (Position / _effectedObjects.Count);
        }

        public void Start(PivotType pivotType, GizmoMode mode)
        {
            _totalGizomTransform = Matrix.Identity;
            var d = _effectedObjects.Select(x => x.Geometry).ToList();
            _activeCommand = new TransformVertexCommand2(d, Position, pivotType == PivotType.ObjectCenter, mode == GizmoMode.Rotate);
        }

        public void Stop(CommandExecutor commandManager)
        {
            if (_activeCommand != null)
            {
                _activeCommand.Transform = _totalGizomTransform;
                commandManager.ExecuteCommand(_activeCommand);
            }
        }

        public void GizmoTranslateEvent(TransformationEventArgs e)
        {
            foreach(var obj in _effectedObjects)
            {
                var geo = obj.Geometry;

                for (int i = 0; i < geo.VertexCount(); i++)
                {
                    var vert = geo.GetVertexById(i);
                    vert = Vector3.Transform(vert, Matrix.CreateTranslation((Vector3)e.Value));
                    geo.UpdateVertexPosition(i, vert);
                }
                geo.RebuildVertexBuffer();
            }

            Position += (Vector3)e.Value;
            _totalGizomTransform *= Matrix.CreateTranslation((Vector3)e.Value);
        }

        public void GizmoRotateEvent(TransformationEventArgs e)
        {
            foreach (var obj in _effectedObjects)
            {
                var objCenter = Vector3.Zero;
                if (e.Pivot == PivotType.ObjectCenter)
                    objCenter = Position;
               
                var geo = obj.Geometry;
                for (int i = 0; i < geo.VertexCount(); i++)
                {
                    var vert = geo.GetVertexById(i);
                    vert = vert - objCenter;
                    vert = Vector3.Transform(vert, (Matrix)e.Value); // Rotate normal, bi normals and all that shit
                    vert = vert + objCenter;
                    geo.UpdateVertexPosition(i, vert);
                }
                geo.RebuildVertexBuffer();
            }

            //Orientation += Quaternion.CreateFromRotationMatrix((Matrix)e.Value);
            _totalGizomTransform *= (Matrix)e.Value;
        }

        public void GizmoScaleEvent(TransformationEventArgs e)
        {
            var scalefactor = (Vector3)e.Value;


            scalefactor = (scalefactor) + Vector3.One;
  
            var scaleMatrix = Matrix.CreateScale(scalefactor);
            foreach (var obj in _effectedObjects)
            {
                var objCenter = Vector3.Zero;
                if (e.Pivot == PivotType.ObjectCenter)
                    objCenter = Position;

                var geo = obj.Geometry;
                for (int i = 0; i < geo.VertexCount(); i++)
                {
                    var vert = geo.GetVertexById(i);
                    vert = vert - objCenter;
                    vert = Vector3.Transform(vert, scaleMatrix); // Rotate normal, bi normals and all that shit
                    vert = vert + objCenter;
                    geo.UpdateVertexPosition(i, vert);
                }
                geo.RebuildVertexBuffer();
            }

            Scale += (Vector3)e.Value;
            _totalGizomTransform *= scaleMatrix;
        }

        public Vector3 GetObjectCenter()
        {
            return Position;
        }
    }
}
