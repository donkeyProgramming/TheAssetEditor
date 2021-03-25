using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Face;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.SceneNodes;
using View3D.Services;

namespace View3D.Components.Component
{
    public class FaceEditor : BaseComponent
    {
        CommandExecutor _commandManager;
        SelectionManager _selectionManager;

        public FaceEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _commandManager = GetComponent<CommandExecutor>();
            _selectionManager = GetComponent<SelectionManager>();
            base.Initialize();
        }

        public void DeleteFaces(FaceSelectionState faceSelectionState)
        {
            var selectedFaceCount = faceSelectionState.CurrentSelection().Count() * 3;
            var totalObjectFaceCount = faceSelectionState.RenderObject.Geometry.GetIndexCount();

            if (selectedFaceCount == totalObjectFaceCount)
            {
                var command = new DeleteObjectsCommand(new List<ISelectable>() { faceSelectionState.RenderObject });
                _commandManager.ExecuteCommand(command);
            }
            else
            {
                var command = new DeleteFaceCommand(faceSelectionState.RenderObject.Geometry, faceSelectionState.CurrentSelection());
                _commandManager.ExecuteCommand(command);
            }
        }

        public void GrowSelection(FaceSelectionState faceSelectionState)
        {
            var selectedFaceIndecies = new List<ushort>();
            var indexBuffer = faceSelectionState.RenderObject.Geometry.GetIndexBuffer();
            foreach (var face in faceSelectionState.SelectedFaces)
            {
                selectedFaceIndecies.Add(indexBuffer[face]);
                selectedFaceIndecies.Add(indexBuffer[face + 1]);
                selectedFaceIndecies.Add(indexBuffer[face + 2]);
            }

            var meshService = new MeshSplitterService();
            var newSelection = meshService.GrowSelection(faceSelectionState.RenderObject.Geometry, selectedFaceIndecies);

            var selectCmd = new FaceSelectionCommand(newSelection);
            _commandManager.ExecuteCommand(selectCmd);
        }

        public void DuplicatedSelectedFacesToNewMesh(FaceSelectionState faceSelectionState, bool deleteOriginal)
        {
            var selectCmd = new DuplicateFacesCommand(faceSelectionState.RenderObject, faceSelectionState.SelectedFaces, deleteOriginal);
            _commandManager.ExecuteCommand(selectCmd);
        }

        public void ConvertSelectionToVertex(FaceSelectionState faceSelectionState)
        {
            var selectCmd = new ConvertFacesToVertexSelectionCommand(faceSelectionState);
            _commandManager.ExecuteCommand(selectCmd);
        }
    }
}
