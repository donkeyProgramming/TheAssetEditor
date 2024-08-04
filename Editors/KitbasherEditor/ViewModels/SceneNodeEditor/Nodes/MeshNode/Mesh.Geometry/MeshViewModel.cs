using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2
{
    public partial class MeshViewModel : ObservableObject, IDisposable
    {
        Rmv2MeshNode _meshNode;
        private readonly SceneManager _sceneManager;

        public string ModelName { get { return _meshNode.Material.ModelName; } set { _meshNode.Material.ModelName = value; } }
        public string ShaderName { get => _meshNode.CommonHeader.ShaderParams.ShaderName; }
        public int VertexCount { get => _meshNode.Geometry.VertexCount(); }
        public int IndexCount { get => _meshNode.Geometry.GetIndexCount(); }
        public bool DrawBoundingBox { get { return _meshNode.DisplayBoundingBox; } set { _meshNode.DisplayBoundingBox = value; } }
        public bool DrawPivotPoint { get { return _meshNode.DisplayPivotPoint; } set { _meshNode.DisplayPivotPoint = value; } }
        public bool ReduceMeshOnLodGeneration { get { return _meshNode.ReduceMeshOnLodGeneration; } set { _meshNode.ReduceMeshOnLodGeneration = value; } }
        public Vector3ViewModel Pivot { get; set; } = new Vector3ViewModel(0);
        public ModelMaterialEnum SelectedMaterialType { get => _meshNode.CommonHeader.ModelTypeFlag; set { } }

        public MeshViewModel(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Initialize(Rmv2MeshNode node)
        {
            _meshNode = node;

            Pivot.Set(_meshNode.Material.PivotPoint);
            Pivot.OnValueChanged += Pivot_OnValueChanged;
        }

        private void Pivot_OnValueChanged(Vector3ViewModel newValue)
        {
            _meshNode.UpdatePivotPoint(new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value));
        }

        [RelayCommand]
        void CopyPivotToAllMeshes()
        {
            var newPiv = new Vector3((float)Pivot.X.Value, (float)Pivot.Y.Value, (float)Pivot.Z.Value);
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var allMeshes = root.GetMeshesInLod(0, false);
            foreach (var mesh in allMeshes)
                mesh.UpdatePivotPoint(newPiv);
        }

        public void Dispose()
        {
            Pivot.OnValueChanged -= Pivot_OnValueChanged;
        }
    }
}
