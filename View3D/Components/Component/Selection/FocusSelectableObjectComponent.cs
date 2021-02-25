using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.SceneNodes;
using View3D.Utility;

namespace View3D.Components.Component.Selection
{
    public class FocusSelectableObjectComponent : BaseComponent
    {
        ILogger _logger = Logging.Create<FocusSelectableObjectComponent>();

        SelectionManager _selectionManager;
        ArcBallCamera _archballCamera;
        SceneManager _sceneManager;

        public FocusSelectableObjectComponent(WpfGame game) : base(game) { }

        public override void Initialize()
        {
            _selectionManager = GetComponent<SelectionManager>();
            _archballCamera = GetComponent<ArcBallCamera>();
            _sceneManager = GetComponent<SceneManager>();

            base.Initialize();
        }


        public void FocusSelection()
        {
            Focus(_selectionManager.GetState());
        }

        void Focus(ISelectionState selectionState)
        {
            if (selectionState is ObjectSelectionState objectState)
            {
                if (objectState.SelectedObjects().Count == 0)
                    return;
                Vector3 finalPos = Vector3.Zero;
                foreach (var item in objectState.SelectedObjects())
                    finalPos += Vector3.Transform(MathUtil.GetCenter(item.Geometry.BoundingBox), _sceneManager.GetWorldPosition(item));

                _archballCamera.LookAt = finalPos / objectState.SelectedObjects().Count();
            }
            else if (selectionState is VertexSelectionState vertexSelection)
            {
                var vertexList = vertexSelection.SelectedVertices;
                var objectPos = _sceneManager.GetWorldPosition(vertexSelection.RenderObject).Translation;
                if (vertexList.Count == 0)
                {
                    _archballCamera.LookAt = objectPos;
                    return;
                }
               
                var averageVertexPos = Vector3.Zero;
                foreach (var vertexIndex in vertexList)
                    averageVertexPos += vertexSelection.RenderObject.Geometry.GetVertexById(vertexIndex);

                averageVertexPos = averageVertexPos / vertexList.Count;
                _archballCamera.LookAt = averageVertexPos + objectPos;
            }
            else if (selectionState is FaceSelectionState faceSelection)
            {
                var faceList = faceSelection.SelectedFaces;
                var objectPos = _sceneManager.GetWorldPosition(faceSelection.RenderObject).Translation;
                if (faceList.Count == 0)
                {
                    _archballCamera.LookAt = objectPos;
                    return;
                }

                var averageFacePos = Vector3.Zero;
                foreach (var faceIndex in faceList)
                {
                    var index0 = faceSelection.RenderObject.Geometry.GetIndex(faceIndex+0);
                    var index1 = faceSelection.RenderObject.Geometry.GetIndex(faceIndex+1);
                    var index2 = faceSelection.RenderObject.Geometry.GetIndex(faceIndex+2);

                    var face0 = faceSelection.RenderObject.Geometry.GetVertexById(index0);
                    var face1 = faceSelection.RenderObject.Geometry.GetVertexById(index1);
                    var face2 = faceSelection.RenderObject.Geometry.GetVertexById(index2);
                    averageFacePos += (face0 + face1 + face2) / 3;
                }
                averageFacePos = averageFacePos / faceList.Count;
                _archballCamera.LookAt = averageFacePos + objectPos;
            }
        }


        public void ResetCamera()
        {
            _archballCamera.LookAt = Vector3.Zero;
            _archballCamera.Zoom = 10;
        }
    }
}
