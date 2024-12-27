using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.KitbasherEditor.ChildEditors.PinTool.Commands;
using GameWorld.Core.Commands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.Services;

namespace Editors.KitbasherEditor.ChildEditors.PinTool
{
    public partial class PinRiggingAlgorithm : ObservableObject
    {
        private readonly IStandardDialogs _standardDialogs;
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        [ObservableProperty] List<int> _selectedVertex =[];
        [ObservableProperty] Rmv2MeshNode _selectedMesh;
        [ObservableProperty] string _description = "";

        public PinRiggingAlgorithm(CommandFactory commandFactory, IStandardDialogs standardDialogs, SelectionManager selectionManager)
        {
            _commandFactory = commandFactory;
            _standardDialogs = standardDialogs;
            _selectionManager = selectionManager;
        }

        public bool Execute(List<Rmv2MeshNode> meshesToAffect)
        {
            if (SelectedMesh == null || SelectedVertex.Count == 0)
            {
                _standardDialogs.ShowDialogBox("No mesh or vertex selected", "Error");
                return false;
            }

            if (meshesToAffect.Any(x => x == SelectedMesh))
            {
                _standardDialogs.ShowDialogBox("Source mesh is also in the list of target meshes", "Error");
                return false;
            }

            _commandFactory.Create<PinMeshToVertexCommand>().Configure(x => x.Configure(meshesToAffect, SelectedMesh, SelectedVertex.First())).BuildAndExecute();
            return true;
        }

        [RelayCommand] void SetSelection()
        {
            SelectedVertex.Clear();
            SelectedMesh = null;

            var description = "No Mesh selected";
            var selectionState = _selectionManager.GetState<VertexSelectionState>();
            if (selectionState == null || selectionState.SelectionCount() == 0)
            {
                _standardDialogs.ShowDialogBox("No vertex selected", "Error");
                return;
            }
            
            var selectionAsMeshNode = selectionState.GetSingleSelectedObject() as Rmv2MeshNode;
            if (selectionAsMeshNode == null)
                throw new Exception($"Unexpected result for selection. State = {selectionState}");

            if (selectionAsMeshNode.PivotPoint != Vector3.Zero)
            {
                _standardDialogs.ShowDialogBox("Selected mesh has a pivot point, the tool will not work correctly", "error");
                return;
            }
            
            SelectedMesh = selectionAsMeshNode;
            SelectedVertex = selectionState.SelectedVertices.ToList();

            description = $"Mesh:{SelectedMesh.Name}', Num Verts: {SelectedVertex.Count}";
            Description = description;
        }
    }
}
