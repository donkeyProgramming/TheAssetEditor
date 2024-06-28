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
    internal class SaveDialogViewModel : NotifyPropertyChangedImpl 
    {
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private readonly PackFileService _pfs;
        private readonly SaveSettings _saveSettings;

        public ObservableCollection<LodGroupNodeViewModel> LodNodes { get; set; } = []; 
        public List<ComboBoxItem<GeometryStrategy>> MeshStrategies { get; set; } 
        public List<ComboBoxItem<MaterialStrategy>> WsStrategies { get; set; } 
        public List<ComboBoxItem<LodStrategy>> LodStrategies { get; set; }
        public List<int> PossibleLodNumbers { get; set; } = [1,4,5];
   

        public NotifyAttr<string> OutputPath { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<ComboBoxItem<GeometryStrategy>> SelectedMeshStrategy { get; set; } = new NotifyAttr<ComboBoxItem<GeometryStrategy>>();
        public NotifyAttr<ComboBoxItem<MaterialStrategy>> SelectedWsModelStrategy { get; set; } = new NotifyAttr<ComboBoxItem<MaterialStrategy>>();
        public NotifyAttr<ComboBoxItem<LodStrategy>> SelectedLodStrategy { get; set; } = new NotifyAttr<ComboBoxItem<LodStrategy>>();
        public NotifyAttr<bool> OnlySaveVisible { get; set; } = new NotifyAttr<bool>(false);

        int _numberOfLodsToGenerate;
        public int NumberOfLodsToGenerate { get => _numberOfLodsToGenerate; set => SetAndNotify(ref _numberOfLodsToGenerate, value, NumberOfLodsChanged); }

        public SaveDialogViewModel(SceneManager sceneManager, SaveService saveService, PackFileService pfs, SaveSettings saveSettings)
        {
            _sceneManager = sceneManager;
            _saveService = saveService;
            _pfs = pfs;
            _saveSettings = saveSettings;

            MeshStrategies = _saveService.GetGeometryStrategies().Select(x => new ComboBoxItem<GeometryStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            WsStrategies = _saveService.GetMaterialStrategies().Select(x => new ComboBoxItem<MaterialStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            LodStrategies = _saveService.GetLodStrategies().Select(x => new ComboBoxItem<LodStrategy>(x.StrategyId, x.Name, x.Description)).ToList();

            Initialize(saveSettings);
        }

        internal void Initialize(SaveSettings settings)
        {
            OutputPath.Value = settings.OutputName;
            SelectedMeshStrategy.Value = MeshStrategies.First(x => x.Value == settings.GeometryOutputType);
            SelectedWsModelStrategy.Value = WsStrategies.First(x => x.Value == settings.MaterialOutputType);
            SelectedLodStrategy.Value = LodStrategies.First(x => x.Value == settings.LodGenerationMethod);
            OnlySaveVisible.Value = settings.OnlySaveVisible;
            NumberOfLodsToGenerate = settings.NumberOfLodsToGenerate;

            BuildLodOverview(settings);

            settings.IsUserInitialized = true;
        }

        void NumberOfLodsChanged(int newValue) 
        {
            _saveSettings.NumberOfLodsToGenerate = newValue;
            _saveSettings.RefreshLodSettings();
            BuildLodOverview(_saveSettings);
        }

        void BuildLodOverview(SaveSettings saveSettings)
        {
            LodNodes.Clear();
            var lodNodesInModel = _sceneManager
                .GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel)
                .GetLodNodes();

            for (var i = 0; i < _saveSettings.NumberOfLodsToGenerate; i++)
            {
                Rmv2LodNode? lodNode = null;
                if(i > lodNodesInModel.Count)
                    lodNode = lodNodesInModel[i];
                LodNodes.Add(new LodGroupNodeViewModel(lodNode, i, saveSettings));
            }
        }

        public void HandleApply(ref SaveSettings saveSettings)
        {
            saveSettings.OutputName = OutputPath.Value;
            saveSettings.OnlySaveVisible = OnlySaveVisible.Value;
            saveSettings.GeometryOutputType = SelectedMeshStrategy.Value.Value;
            saveSettings.MaterialOutputType = SelectedWsModelStrategy.Value.Value;
            saveSettings.LodGenerationMethod = SelectedLodStrategy.Value.Value;
            saveSettings.NumberOfLodsToGenerate = NumberOfLodsToGenerate;
        }

        public void HandleBrowseLocation()
        {
            var extension = ".rigid_model_v2";
            var dialogResult = _pfs.UiProvider.DisplaySaveDialog(_pfs, [extension], out _, out var filePath);

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
       
       internal void UpdateSettings(ref SaveSettings settings)
       {
           HandleApply(ref settings);
       }
    }
}
