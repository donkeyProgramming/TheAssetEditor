using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommonControls.Common;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.PinTool
{
    public class PinToolViewModel
    {
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        public ObservableCollection<Rmv2MeshNode> AffectedMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();
        public ObservableCollection<Rmv2MeshNode> SourceMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();

        Rmv2MeshNode _selectedVertexMesh;
        List<int> _selectedVertexList = new();

        public NotifyAttr<string> SelectedForStaticMeshName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> SelectedForStaticDescription { get; set; } = new NotifyAttr<string>($"Selected vertex count : ");


        public PinToolViewModel(SelectionManager selectionManager, CommandFactory commandFactory)
        {
            _selectionManager = selectionManager;
            _commandFactory = commandFactory;
        }

        public void ClearSourcedMeshCollection() => SourceMeshCollection.Clear();
        public void AddSelectionToSourceMeshCollection() => AddSelectionToList(SourceMeshCollection);
        public void ClearAffectedMeshCollection() => AffectedMeshCollection.Clear();
        public void AddSelectionToAffectMeshCollection() => AddSelectionToList(AffectedMeshCollection);

        public void SetSelectedVertex()
        {
            _selectedVertexList.Clear();
            _selectedVertexMesh = null;

            SelectedForStaticMeshName.Value = "";
            SelectedForStaticDescription.Value = $"Selected vertex count : ";

            var selectionState = _selectionManager.GetState<VertexSelectionState>();

            if (selectionState == null || selectionState.SelectionCount() == 0)
            {
                MessageBox.Show("No vertex selected", "Error");
            }
            else
            {
                _selectedVertexMesh = selectionState.GetSingleSelectedObject() as Rmv2MeshNode;
                _selectedVertexList = selectionState.SelectedVertices.ToList();

                SelectedForStaticMeshName.Value = _selectedVertexMesh.Name;
                SelectedForStaticDescription.Value = $"Num selected verts: {_selectedVertexList.Count}";
            }
        }

        void AddSelectionToList(ObservableCollection<Rmv2MeshNode> itemList)
        {
            var selectionState = _selectionManager.GetState<ObjectSelectionState>();

            if (selectionState == null)
            {
                MessageBox.Show("Please select objects", "Error");
                return;
            }

            var selectedObjects = selectionState.SelectedObjects()
                .Select(x => x as Rmv2MeshNode)
                .Where(x => x != null)
                .ToList();

            foreach (var item in itemList)
                selectedObjects.Add(item);

            var sortedObjects = selectedObjects
                .Distinct()
                .OrderByDescending(x => x.Name);

            itemList.Clear();
            foreach (var item in sortedObjects)
                itemList.Add(item);
        }

        public void Apply()
        {
            if (AffectedMeshCollection.Count == 0)
            {
                MessageBox.Show("No meshes to be affected seleceted", "Error");
                return;
            }

           
            ApplyPintToPoint();
        }

        void ApplyPintToPoint()
        {
            if (_selectedVertexMesh == null || _selectedVertexList.Count == 0)
            {
                MessageBox.Show("No mesh or vertex selected", "Error");
                return;
            }

            if (AffectedMeshCollection.Any(x => x == _selectedVertexMesh))
            {
                MessageBox.Show("Source mesh is also in the list of target meshes", "Error");
                return;
            }

            _commandFactory.Create<PinMeshToVertexCommand>().Configure(x => x.Configure(AffectedMeshCollection, _selectedVertexMesh, _selectedVertexList.First())).BuildAndExecute();
        }
    }
}
