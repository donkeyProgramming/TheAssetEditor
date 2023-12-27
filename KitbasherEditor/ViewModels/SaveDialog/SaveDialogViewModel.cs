using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommonControls.BaseDialogs;
using CommonControls.Common;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Services.SceneSaving;
using View3D.Services.SceneSaving.Geometry;
using View3D.Services.SceneSaving.Lod;
using View3D.Services.SceneSaving.WsModel;

namespace KitbasherEditor.ViewModels.SaveDialog
{
    internal class SaveDialogViewModel : NotifyPropertyChangedImpl
    {
        private readonly SaveSettings _settings;
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private IAssetEditorWindow _parentWindow;

        public ObservableCollection<LodGroupNodeViewModel> LodNodes { get; set; } = new ObservableCollection<LodGroupNodeViewModel>();  // Move to own class
        public List<ComboBoxItem<GeometryStrategy>> MeshStrategies { get; set; } 
        public List<ComboBoxItem<MaterialStrategy>> WsStrategies { get; set; } 
        public List<ComboBoxItem<LodStrategy>> LodStrategies { get; set; } 

        public NotifyAttr<string> OutputPath { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<ComboBoxItem<GeometryStrategy>> SelectedMeshStrategy { get; set; } = new NotifyAttr<ComboBoxItem<GeometryStrategy>>();
        public NotifyAttr<ComboBoxItem<MaterialStrategy>> SelectedWsModelStrategy { get; set; } = new NotifyAttr<ComboBoxItem<MaterialStrategy>>();
        public NotifyAttr<ComboBoxItem<LodStrategy>> SelectedLodStrategy { get; set; } = new NotifyAttr<ComboBoxItem<LodStrategy>>();
        public NotifyAttr<bool> OnlySaveVisible { get; set; } = new NotifyAttr<bool>(false);

        public SaveDialogViewModel(SaveSettings settings, SceneManager sceneManager, SaveService saveService)
        {
            _settings = settings;
            _sceneManager = sceneManager;
            _saveService = saveService;

            MeshStrategies = _saveService.GetGeometryStrategies().Select(x => new ComboBoxItem<GeometryStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            WsStrategies = _saveService.GetMaterialStrategies().Select(x => new ComboBoxItem<MaterialStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            LodStrategies = _saveService.GetLodStrategies().Select(x => new ComboBoxItem<LodStrategy>(x.StrategyId, x.Name, x.Description)).ToList();

            OutputPath.Value = _settings.OutputName;
            SelectedMeshStrategy.Value = MeshStrategies.First(x => x.Value == _settings.GeometryOutputType);
            SelectedWsModelStrategy.Value = WsStrategies.First(x => x.Value == _settings.MaterialOutputType);
            SelectedLodStrategy.Value = LodStrategies.First(x => x.Value == _settings.LodGenerationMethod);
            OnlySaveVisible.Value = _settings.OnlySaveVisible;

            BuildLodOverview();

            _settings.IsInitialized = true;
        }

        public void Initialise(IAssetEditorWindow parentWindow)
        {
            _parentWindow = parentWindow;
        }

        void BuildLodOverview()
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            LodNodes.Clear();
            foreach (var lodNode in mainNode.GetLodNodes())
                LodNodes.Add(new LodGroupNodeViewModel(lodNode, _settings));
        }

        public void HandleApply()
        {
            _settings.OutputName = OutputPath.Value;
            _settings.OnlySaveVisible = OnlySaveVisible.Value;
            _settings.GeometryOutputType = SelectedMeshStrategy.Value.Value;
            _settings.MaterialOutputType = SelectedWsModelStrategy.Value.Value;
            _settings.LodGenerationMethod = SelectedLodStrategy.Value.Value;
  
            // Update meshes based on lod settings
            // Trigger mesh save 
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            _saveService.Save(mainNode, _settings);
        }

        public void HandleSave()
        {
            HandleApply();
            HandleClose();
        }

        public void HandleClose() => _parentWindow.CloseWindow();
    }
}
