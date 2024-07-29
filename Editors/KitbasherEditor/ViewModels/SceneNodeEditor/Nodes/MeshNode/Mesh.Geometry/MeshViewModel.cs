using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2
{
    public partial class MeshViewModel : ObservableObject
    {
        Rmv2MeshNode _meshNode;
        private readonly SceneManager _sceneManager;
        private readonly KitbasherRootScene _kitbasherRootScene;

        public Vector3ViewModel Pivot { get; set; }

        [ObservableProperty] public string _modelName; 
        [ObservableProperty] public bool _drawBoundingBox; 
        [ObservableProperty] public bool _drawPivotPoint;
        [ObservableProperty] bool _reduceMeshOnLodGeneration ;
        [ObservableProperty] string _shaderName;
        [ObservableProperty] int _vertexCount;
        [ObservableProperty] int _indexCount;
        [ObservableProperty] ModelMaterialEnum _selectedMaterialType;
        [ObservableProperty] UiVertexFormat _vertexType;
        [ObservableProperty] IEnumerable<UiVertexFormat> _possibleVertexTypes = [UiVertexFormat.Static, UiVertexFormat.Weighted, UiVertexFormat.Cinematic];


        public MeshViewModel(KitbasherRootScene kitbasherRootScene, SceneManager sceneManager)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _sceneManager = sceneManager;
        }

        public void Initialize(Rmv2MeshNode node)
        {
            _meshNode = node;

            Pivot = new Vector3ViewModel(_meshNode.Material.PivotPoint, Pivot_OnValueChanged);
            ModelName = _meshNode.Material.ModelName;
            DrawBoundingBox = _meshNode.DisplayBoundingBox;
            DrawPivotPoint = _meshNode.DisplayPivotPoint;
            ShaderName = _meshNode.CommonHeader.ShaderParams.ShaderName;
            VertexCount = _meshNode.Geometry.VertexCount();
            IndexCount = _meshNode.Geometry.GetIndexCount();
            SelectedMaterialType = _meshNode.CommonHeader.ModelTypeFlag;
            ReduceMeshOnLodGeneration = _meshNode.ReduceMeshOnLodGeneration;
        }

        partial void OnModelNameChanged(string value) => _meshNode.Material.ModelName = value;
        partial void OnDrawBoundingBoxChanged(bool value) => _meshNode.DisplayBoundingBox = value;
        partial void OnDrawPivotPointChanged(bool value) => _meshNode.DisplayBoundingBox = value;
        partial void OnReduceMeshOnLodGenerationChanged(bool value) => _meshNode.ReduceMeshOnLodGeneration = value;
        partial void OnVertexTypeChanged(UiVertexFormat value) => _meshNode.Geometry.ChangeVertexType(value, _kitbasherRootScene.Skeleton.SkeletonName);
        private void Pivot_OnValueChanged(Vector3 newValue) => _meshNode.UpdatePivotPoint(newValue);
        

        [RelayCommand]
        void CopyPivotToAllMeshes()
        {
            var newPiv = new Vector3((float)Pivot.X.Value, (float)Pivot.Y.Value, (float)Pivot.Z.Value);
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var allMeshes = root.GetMeshesInLod(0, false);
            foreach (var mesh in allMeshes)
                mesh.UpdatePivotPoint(newPiv);
        }
    }
}
