using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.GameFormats.RigidModel;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2
{
    public class MeshViewModel : NotifyPropertyChangedImpl, IDisposable
    {
        Rmv2MeshNode _meshNode;
        private readonly SceneManager _sceneManager;

        public string ModelName { get { return _meshNode.Material.ModelName; } set { _meshNode.Material.ModelName = value; NotifyPropertyChanged(); } }
        public string ShaderName { get => _meshNode.CommonHeader.ShaderParams.ShaderName; }
        public int VertexCount { get => _meshNode.Geometry.VertexCount(); }
        public int IndexCount { get => _meshNode.Geometry.GetIndexCount(); }

        public bool DrawBoundingBox { get { return _meshNode.DisplayBoundingBox; } set { _meshNode.DisplayBoundingBox = value; NotifyPropertyChanged(); } }
        public bool DrawPivotPoint { get { return _meshNode.DisplayPivotPoint; } set { _meshNode.DisplayPivotPoint = value; NotifyPropertyChanged(); } }
        public bool ReduceMeshOnLodGeneration { get { return _meshNode.ReduceMeshOnLodGeneration; } set { _meshNode.ReduceMeshOnLodGeneration = value; NotifyPropertyChanged(); } }

        Vector3ViewModel _pivot;
        public Vector3ViewModel Pivot { get { return _pivot; } set { SetAndNotify(ref _pivot, value); } }

        public ModelMaterialEnum SelectedMaterialType { get { return _meshNode.CommonHeader.ModelTypeFlag; } set { NotifyPropertyChanged(); } }

        public ICommand CopyPivotToAllMeshesCommand { get; set; }

        public MeshViewModel(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
            CopyPivotToAllMeshesCommand = new RelayCommand(CopyPivotToAllMeshes);
        }

        public void Initialize(Rmv2MeshNode node)
        {
            _meshNode = node;
            _meshNode.Name = _meshNode.Material.ModelName;

            Pivot = new Vector3ViewModel(_meshNode.Material.PivotPoint);
            Pivot.OnValueChanged += Pivot_OnValueChanged;
        }

        private void Pivot_OnValueChanged(Vector3ViewModel newValue)
        {
            _meshNode.UpdatePivotPoint(new Vector3((float)newValue.X.Value, (float)newValue.Y.Value, (float)newValue.Z.Value));
        }

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
