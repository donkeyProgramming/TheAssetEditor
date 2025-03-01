using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.KitbasherEditor.Core;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
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
        [ObservableProperty] int _vertexCount;
        [ObservableProperty] int _indexCount;
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

            Pivot = new Vector3ViewModel(_meshNode.PivotPoint, Pivot_OnValueChanged);
            ModelName = _meshNode.Name;
            DrawBoundingBox = _meshNode.DisplayBoundingBox;
            DrawPivotPoint = _meshNode.DisplayPivotPoint;
      
            VertexCount = _meshNode.Geometry.VertexCount();
            IndexCount = _meshNode.Geometry.GetIndexCount();

            VertexType = _meshNode.Geometry.VertexFormat;
            ReduceMeshOnLodGeneration = _meshNode.ReduceMeshOnLodGeneration;
        }

        partial void OnModelNameChanged(string value) => _meshNode.Name = value;
        partial void OnDrawBoundingBoxChanged(bool value) => _meshNode.DisplayBoundingBox = value;
        partial void OnDrawPivotPointChanged(bool value) => _meshNode.DisplayPivotPoint = value;
        partial void OnReduceMeshOnLodGenerationChanged(bool value) => _meshNode.ReduceMeshOnLodGeneration = value;
        partial void OnVertexTypeChanged(UiVertexFormat value) => _meshNode.Geometry.ChangeVertexType(value);
        private void Pivot_OnValueChanged(Vector3 newValue) => _meshNode.PivotPoint = newValue;
        

        [RelayCommand]
        void CopyPivotToAllMeshes()
        {
            var newPiv = new Vector3((float)Pivot.X.Value, (float)Pivot.Y.Value, (float)Pivot.Z.Value);
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var allMeshes = root.GetMeshesInLod(0, false);
            foreach (var mesh in allMeshes)
                mesh.PivotPoint = newPiv;
        }
    }
}
