using System.Collections.ObjectModel;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace KitbasherEditor.ViewModels.SaveDialog
{
    internal class SaveDialogViewModel : NotifyPropertyChangedImpl // Rename to MeshSaveSettingsDialogViewModel
    {
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private readonly PackFileService _pfs;

        public ObservableCollection<LodGroupNodeViewModel> LodNodes { get; set; } = new ObservableCollection<LodGroupNodeViewModel>();  // Move to own class
        public List<ComboBoxItem<GeometryStrategy>> MeshStrategies { get; set; } 
        public List<ComboBoxItem<MaterialStrategy>> WsStrategies { get; set; } 
        public List<ComboBoxItem<LodStrategy>> LodStrategies { get; set; } 

        public NotifyAttr<string> OutputPath { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<ComboBoxItem<GeometryStrategy>> SelectedMeshStrategy { get; set; } = new NotifyAttr<ComboBoxItem<GeometryStrategy>>();
        public NotifyAttr<ComboBoxItem<MaterialStrategy>> SelectedWsModelStrategy { get; set; } = new NotifyAttr<ComboBoxItem<MaterialStrategy>>();
        public NotifyAttr<ComboBoxItem<LodStrategy>> SelectedLodStrategy { get; set; } = new NotifyAttr<ComboBoxItem<LodStrategy>>();
        public NotifyAttr<bool> OnlySaveVisible { get; set; } = new NotifyAttr<bool>(false);

        public SaveDialogViewModel(SceneManager sceneManager, SaveService saveService, PackFileService pfs)
        {
            _sceneManager = sceneManager;
            _saveService = saveService;
            _pfs = pfs;

            MeshStrategies = _saveService.GetGeometryStrategies().Select(x => new ComboBoxItem<GeometryStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            WsStrategies = _saveService.GetMaterialStrategies().Select(x => new ComboBoxItem<MaterialStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            LodStrategies = _saveService.GetLodStrategies().Select(x => new ComboBoxItem<LodStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
        }

        void BuildLodOverview(SaveSettings saveSettings)
        {
            var mainNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lodNodesInModel = mainNode.GetLodNodes();

            LodNodes.Clear();
            foreach (var lodNode in lodNodesInModel)
                LodNodes.Add(new LodGroupNodeViewModel(lodNode, saveSettings));
        }

        public void HandleApply(ref SaveSettings saveSettings)
        {
            saveSettings.OutputName = OutputPath.Value;
            saveSettings.OnlySaveVisible = OnlySaveVisible.Value;
            saveSettings.GeometryOutputType = SelectedMeshStrategy.Value.Value;
            saveSettings.MaterialOutputType = SelectedWsModelStrategy.Value.Value;
            saveSettings.LodGenerationMethod = SelectedLodStrategy.Value.Value;
        }

        public void HandleBrowseLocation()
        {
            var extension = ".rigid_model_v2";
            var dialogResult = _pfs.UiProvider.DisplaySaveDialog(_pfs, new List<string>() { extension }, out _, out var filePath);

            if (dialogResult == true)
            {
                var path = filePath!;
                if (path.Contains(extension) == false)
                    path += extension;

                OutputPath.Value = path;
            }
        }

        public void HandleSave(ref SaveSettings saveSettings)
        {
            HandleApply(ref saveSettings);
        }

        internal void Initialize(SaveSettings settings)
        {
            OutputPath.Value = settings.OutputName;
            SelectedMeshStrategy.Value = MeshStrategies.First(x => x.Value == settings.GeometryOutputType);
            SelectedWsModelStrategy.Value = WsStrategies.First(x => x.Value == settings.MaterialOutputType);
            SelectedLodStrategy.Value = LodStrategies.First(x => x.Value == settings.LodGenerationMethod);
            OnlySaveVisible.Value = settings.OnlySaveVisible;

            BuildLodOverview(settings);

            settings.IsUserInitialized = true;
        }

        internal void UpdateSettings(ref SaveSettings settings)
        {
            HandleApply(ref settings);
        }
    }
}
