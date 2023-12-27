using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.MathViews;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class MeshViewModel : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;
        private readonly SceneManager _sceneManager;
        IComponentManager _componentManager;

        public string ModelName { get { return _meshNode.Material.ModelName; } set { _meshNode.Material.ModelName = value; NotifyPropertyChanged(); } }
        public string ShaderName { get => _meshNode.CommonHeader.ShaderParams.ShaderName; }
        public int VertexCount { get => _meshNode.Geometry.VertexCount(); }
        public int IndexCount { get => _meshNode.Geometry.GetIndexCount(); }

        public bool DrawBoundingBox { get { return _meshNode.DisplayBoundingBox; } set { _meshNode.DisplayBoundingBox = value; NotifyPropertyChanged(); } }
        public bool DrawPivotPoint { get { return _meshNode.DisplayPivotPoint; } set { _meshNode.DisplayPivotPoint = value; NotifyPropertyChanged(); } }
        public bool ReduceMeshOnLodGeneration { get { return _meshNode.ReduceMeshOnLodGeneration; } set { _meshNode.ReduceMeshOnLodGeneration = value; NotifyPropertyChanged(); } }

        Vector3ViewModel _pivot;
        public Vector3ViewModel Pivot { get { return _pivot; } set { SetAndNotify(ref _pivot, value); } }

        public IEnumerable<ModelMaterialEnum> PossibleMaterialTypes { get; set; }
        public ModelMaterialEnum SelectedMaterialType { get { return _meshNode.CommonHeader.ModelTypeFlag; } set { UpdateGroupType(value); NotifyPropertyChanged(); } }

        public ICommand CopyPivotToAllMeshesCommand { get; set; }

        public MeshViewModel(Rmv2MeshNode node, SceneManager sceneManager)
        {
            _meshNode = node;
            _sceneManager = sceneManager;
            _meshNode.Name = _meshNode.Material.ModelName;

            PossibleMaterialTypes = MaterialFactory.Create().GetSupportedMaterials();

            Pivot = new Vector3ViewModel(_meshNode.Material.PivotPoint);
            Pivot.OnValueChanged += Pivot_OnValueChanged;

            CopyPivotToAllMeshesCommand = new RelayCommand(CopyPivotToAllMeshes);
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

        private void UpdateGroupType(ModelMaterialEnum value)
        {
            MessageBox.Show("Changing material type is currently not supported - Default/weighted swaps happen by default when saving.\nDid you mean to change vertex type?");
        }
    }
}
