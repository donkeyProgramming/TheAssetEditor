using CommonControls.Common;
using CommonControls.Simple;
using KitbasherEditor.Views.EditorViews.PinTool;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.PinTool
{
    public class PinToolViewModel
    {
        IComponentManager _componentManager;

        public NotifyAttr<bool> IsPintToPointMode { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<bool> IsSkinwrapMode { get; set; } = new NotifyAttr<bool>(false);

        public ObservableCollection<Rmv2MeshNode> AffectedMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();
        public ObservableCollection<Rmv2MeshNode> SourceMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();

        Rmv2MeshNode _selectedVertexMesh;
        List<int> _selectedVertexList = new List<int>();

        public NotifyAttr<string> SelectedForStaticMeshName { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> SelectedForStaticDescription { get; set; } = new NotifyAttr<string>($"Selected vertex count : ");


        public PinToolViewModel(IComponentManager componentManager)
        {
            _componentManager = componentManager;
        }

        public void ClearSourcedMeshCollection() => SourceMeshCollection.Clear();
        public void AddSelectionToSourceMeshCollection() =>  AddSelectionToList(SourceMeshCollection);
        public void ClearAffectedMeshCollection() => AffectedMeshCollection.Clear();
        public void AddSelectionToAffectMeshCollection() => AddSelectionToList(AffectedMeshCollection);

        public void SetSelectedVertex()
        {
            _selectedVertexList.Clear();
            _selectedVertexMesh = null;

            SelectedForStaticMeshName.Value = "";
            SelectedForStaticDescription.Value = $"Selected vertex count : ";

            var selectionState = _componentManager
                       .GetComponent<SelectionManager>()
                       .GetState<VertexSelectionState>();

            if (selectionState == null || selectionState.SelectionCount() == 0)
            {
                MessageBox.Show("No vertex selected", "Error");
            }
            else
            {
                _selectedVertexMesh = selectionState.GetSingleSelectedObject() as Rmv2MeshNode;
                _selectedVertexList = selectionState.SelectedVertices.ToList();

                SelectedForStaticMeshName.Value = _selectedVertexMesh.Name;
                SelectedForStaticDescription.Value = $"Selected vertex count : {_selectedVertexList.Count}";
            }
        }


        void AddSelectionToList(ObservableCollection<Rmv2MeshNode> itemList)
        {
            var selectionState = _componentManager
                   .GetComponent<SelectionManager>()
                   .GetState<ObjectSelectionState>();

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

            if (AffectedMeshCollection.Count(x => x == _selectedVertexMesh) != 0)
            {
                MessageBox.Show("Source mesh is also in the list of target meshes", "Error");
                return;
            }

             var cmd = new PinMeshToVertexCommand(AffectedMeshCollection, _selectedVertexMesh, _selectedVertexList.First());
             var commandExecutor = _componentManager.GetComponent<CommandExecutor>();
             commandExecutor.ExecuteCommand(cmd);
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

            var cmd = new SkinWrapRiggingCommand(AffectedMeshCollection, SourceMeshCollection);
            var commandExecutor = _componentManager.GetComponent<CommandExecutor>();
            commandExecutor.ExecuteCommand(cmd);
        }

        public static void ShowWindow(IComponentManager componentManager)
        {
            var window = new ControllerHostWindow(true)
            {
                DataContext = new PinToolViewModel(componentManager),
                Title = "Pin tool",
                Content = new PinToolView(),
                Width = 360,
                Height = 415,
            };
            window.Show();
        }
    }
}
