using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Face;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Services
{
    public class FaceEditor
    {
        private readonly CommandFactory _commandFactory;

        public FaceEditor(CommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public void DeleteFaces(FaceSelectionState faceSelectionState)
        {
            var selectedFaceCount = faceSelectionState.CurrentSelection().Count() * 3;
            var totalObjectFaceCount = faceSelectionState.RenderObject.Geometry.GetIndexCount();

            if (selectedFaceCount == totalObjectFaceCount)
            {
                _commandFactory.Create<DeleteObjectsCommand>().Configure(x => x.Configure(new List<ISelectable>() { faceSelectionState.RenderObject })).BuildAndExecute();
            }
            else
            {
                _commandFactory.Create<DeleteFaceCommand>().Configure(x => x.Configure(faceSelectionState.RenderObject.Geometry, faceSelectionState.CurrentSelection())).BuildAndExecute();
            }
        }

        public void GrowSelection(FaceSelectionState faceSelectionState, bool combineOverlappingVertexes)
        {
            var selectedFaceIndecies = new List<ushort>();
            var indexBuffer = faceSelectionState.RenderObject.Geometry.GetIndexBuffer();
            foreach (var face in faceSelectionState.SelectedFaces)
            {
                selectedFaceIndecies.Add(indexBuffer[face]);
                selectedFaceIndecies.Add(indexBuffer[face + 1]);
                selectedFaceIndecies.Add(indexBuffer[face + 2]);
            }

            var newSelection = MeshSplitterService.GrowFaceSelection(faceSelectionState.RenderObject.Geometry, selectedFaceIndecies, combineOverlappingVertexes);

            _commandFactory.Create<FaceSelectionCommand>()
               .Configure(x => x.Configure(newSelection))
               .BuildAndExecute();
        }

        public void DuplicatedSelectedFacesToNewMesh(FaceSelectionState faceSelectionState, bool deleteOriginal)
        {
            _commandFactory.Create<DuplicateFacesCommand>()
             .Configure(x => x.Configure(faceSelectionState.RenderObject, faceSelectionState.SelectedFaces, deleteOriginal))
             .BuildAndExecute();
        }

        public void ConvertSelectionToVertex(FaceSelectionState faceSelectionState)
        {
            _commandFactory.Create<ConvertFacesToVertexSelectionCommand>()
                .Configure(x => x.Configure(faceSelectionState))
                .BuildAndExecute();
        }
    }
}
