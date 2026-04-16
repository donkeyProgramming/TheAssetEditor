using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Services
{
    public class FocusSelectableObjectService
    {
        private readonly ILogger _logger = Logging.Create<FocusSelectableObjectService>();
        private readonly SelectionManager _selectionManager;
        private readonly ArcBallCamera _arcBallCamera;
        private readonly SceneManager _sceneManager;

        public FocusSelectableObjectService(SelectionManager selectionManager, ArcBallCamera arcBallCamera, SceneManager sceneManager)
        {
            _selectionManager = selectionManager;
            _arcBallCamera = arcBallCamera;
            _sceneManager = sceneManager;
        }

        public void LookAt(Vector3 position) => _arcBallCamera.LookAt = position;

        public void FocusSelection() => Focus(_selectionManager.GetState());

        public void FocusScene()
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);

            var nodes = mainNode.GetMeshNodes(0)
                .Select(x => x as ISelectable)
                .Where(x => x != null)
                .ToList();

            FocusObjects(nodes);
        }

        public void FocusObjects(List<ISelectable> items)
        {
            if (items.Count == 0)
                return;

            // Create a suber bb
            var bb = items[0].Geometry.BoundingBox;
            for (var i = 1; i < items.Count; i++)
                bb = BoundingBox.CreateMerged(bb, items[i].Geometry.BoundingBox);

            var bbCorners = bb.GetCorners();
            var bbCenter = new Vector3(bbCorners.Average(x => x.X), bbCorners.Average(x => x.Y), bbCorners.Average(x => x.Z));

            double fov = MathHelper.ToRadians(45);
            double boundSphereRadius = bbCorners.Select(x => Vector3.Distance(x, bbCenter)).Max();
            var camDistance = boundSphereRadius * 2.0 / Math.Tan(fov / 2.0) / 2;

            _arcBallCamera.LookAt = bbCenter;
            _arcBallCamera.Zoom = (float)camDistance;
        }

        void Focus(ISelectionState selectionState)
        {
            _logger.Here().Information("Focusing on selection");

            if (selectionState is ObjectSelectionState objectState)
            {
                FocusObjects(objectState.SelectedObjects());
            }
            else if (selectionState is VertexSelectionState vertexSelection)
            {
                var vertexList = vertexSelection.SelectedVertices;
                var objectPos = _sceneManager.GetWorldPosition(vertexSelection.RenderObject).Translation;
                if (vertexList.Count == 0)
                {
                    _arcBallCamera.LookAt = objectPos;
                    return;
                }

                var averageVertexPos = Vector3.Zero;
                foreach (var vertexIndex in vertexList)
                    averageVertexPos += vertexSelection.RenderObject.Geometry.GetVertexById(vertexIndex);

                averageVertexPos = averageVertexPos / vertexList.Count;
                _arcBallCamera.LookAt = averageVertexPos + objectPos;
            }
            else if (selectionState is FaceSelectionState faceSelection)
            {
                var faceList = faceSelection.SelectedFaces;
                var objectPos = _sceneManager.GetWorldPosition(faceSelection.RenderObject).Translation;
                if (faceList.Count == 0)
                {
                    _arcBallCamera.LookAt = objectPos;
                    return;
                }

                var averageFacePos = Vector3.Zero;
                foreach (var faceIndex in faceList)
                {
                    var index0 = faceSelection.RenderObject.Geometry.GetIndex(faceIndex + 0);
                    var index1 = faceSelection.RenderObject.Geometry.GetIndex(faceIndex + 1);
                    var index2 = faceSelection.RenderObject.Geometry.GetIndex(faceIndex + 2);

                    var face0 = faceSelection.RenderObject.Geometry.GetVertexById(index0);
                    var face1 = faceSelection.RenderObject.Geometry.GetVertexById(index1);
                    var face2 = faceSelection.RenderObject.Geometry.GetVertexById(index2);
                    averageFacePos += (face0 + face1 + face2) / 3;
                }
                averageFacePos = averageFacePos / faceList.Count;
                _arcBallCamera.LookAt = averageFacePos + objectPos;
            }
        }


        public void ResetCamera()
        {
            _arcBallCamera.LookAt = Vector3.Zero;
            _arcBallCamera.Zoom = 10;
        }
    }
}
