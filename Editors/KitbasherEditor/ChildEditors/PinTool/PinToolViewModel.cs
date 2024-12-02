using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Editors.KitbasherEditor.ViewModels.PinTool
{
    public class PinToolViewModel : ObservableObject
    {
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;

        Rmv2MeshNode _selectedVertexMesh;
        List<int> _selectedVertexList = new();

        public ObservableCollection<Rmv2MeshNode> AffectedMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();
        public ObservableCollection<Rmv2MeshNode> SourceMeshCollection { get; set; } = new ObservableCollection<Rmv2MeshNode>();

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

   //public class FindClosestPointOnMesh
   //{
   //
   //    public VertexPositionNormalTextureCustom Process(Vector3 inputVertexPosition, MeshObject meshObject)
   //    {
   //        var minDistance = float.MaxValue;
   //        var minPolygonIndex = -1;
   //
   //        var vertexList = meshObject.GetVertexList();
   //        var polygonCount = meshObject.IndexArray.Length / 3;
   //        for (int polygonIndex = 0; polygonIndex < polygonCount; polygonIndex += 3)
   //        {
   //            var i0 = meshObject.IndexArray[polygonIndex + 0];
   //            var i1 = meshObject.IndexArray[polygonIndex + 1];
   //            var i2 = meshObject.IndexArray[polygonIndex + 2];
   //
   //            var v0 = vertexList[i0];
   //            var v1 = vertexList[i1];
   //            var v2 = vertexList[i2];
   //
   //            var closetPoint = MathUtil.ClosestPtPointTriangle(inputVertexPosition, v0, v1, v2);
   //            var dist = Math.Abs(Vector3.DistanceSquared(closetPoint, inputVertexPosition));
   //
   //            if (dist > minDistance)
   //            {
   //                minDistance = dist;
   //                minPolygonIndex = polygonIndex;
   //            }
   //        }
   //
   //        if(minPolygonIndex == -1)
   //        // Find closes polygon
   //        // Find closes point on polygon
   //        // Combyt berry cord 
   //    
   //    
   //    
   //    }
   //
   //
   //
   //
   //}
}
