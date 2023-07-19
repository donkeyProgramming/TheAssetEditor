using CommonControls.BaseDialogs;
using CommonControls.Common;
using KitbasherEditor.Views.EditorViews.PinTool;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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

        public NotifyAttr<bool> IsPintToPointMode { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<bool> IsSkinwrapMode { get; set; } = new NotifyAttr<bool>(false);

        public ObservableCollection<Rmv2MeshNode> AffectedMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();
        public ObservableCollection<Rmv2MeshNode> SourceMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();

        Rmv2MeshNode _selectedVertexMesh;
        List<int> _selectedVertexList = new List<int>();

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

            if (IsPintToPointMode.Value == true)
                ApplyPintToPoint();
            else
                ApplySkinWrapRigging();
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

        void ApplySkinWrapRigging()
        {
            if (SourceMeshCollection.Count == 0 || AffectedMeshCollection.Count == 0)
                return;

            var numSkeletons = SourceMeshCollection.Select(x => x.Geometry.ParentSkeletonName).Distinct().Count();
            var numVertexFormats = SourceMeshCollection.Select(x => x.Geometry.VertexFormat).Distinct().Count();

            if (numSkeletons != 1)
            {
                MessageBox.Show("Multiple skeletons found in the selected source objects, only one type is valid", "Error");
                return;
            }

            if (numVertexFormats != 1)
            {
                MessageBox.Show("Multiple vertex formats found in the selected source objects, only one type is valid", "Error");
                return;
            }

            foreach (var mesh in AffectedMeshCollection)
            {
                if (SourceMeshCollection.Count(x => x == mesh) != 0)
                {
                    MessageBox.Show("Source mesh is also in the list of target meshes", "Error");
                    return;
                }
            }

            _commandFactory.Create<SkinWrapRiggingCommand>().Configure(x => x.Configure(AffectedMeshCollection, SourceMeshCollection)).BuildAndExecute();
        }

        public static void ShowWindow(SelectionManager selectionManager, CommandFactory commandFactory)
        {
            var window = new ControllerHostWindow(true)
            {
                DataContext = new PinToolViewModel(selectionManager, commandFactory),
                Title = "Pin tool",
                Content = new PinToolView(),
                Width = 360,
                Height = 415,
            };
            window.Show();
        }
    }
}
