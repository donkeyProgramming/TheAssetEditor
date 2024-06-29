using System.Collections.ObjectModel;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services.SceneSaving;
using GameWorld.Core.Services.SceneSaving.Geometry;
using GameWorld.Core.Services.SceneSaving.Lod;
using GameWorld.Core.Services.SceneSaving.Material;
using Shared.Core.PackFiles;

namespace KitbasherEditor.ViewModels.SaveDialog
{
    public partial class SaveDialogViewModel : ObservableObject 
    {
        private readonly SceneManager _sceneManager;
        private readonly SaveService _saveService;
        private readonly PackFileService _pfs;
        private SaveSettings? _saveSettings;

        [ObservableProperty] ObservableCollection<LodGroupNodeViewModel> _lodNodes = [];
        [ObservableProperty] List<ComboBoxItem<GeometryStrategy>> _meshStrategies;
        [ObservableProperty] List<ComboBoxItem<MaterialStrategy>> _wsStrategies;
        [ObservableProperty] List<ComboBoxItem<LodStrategy>> _lodStrategies;
        [ObservableProperty] List<int> _possibleLodNumbers  = [1,4,5];

        [ObservableProperty] private string _outputPath;
        [ObservableProperty] private ComboBoxItem<GeometryStrategy> _selectedMeshStrategy;
        [ObservableProperty] private ComboBoxItem<MaterialStrategy> _selectedWsModelStrategy;
        [ObservableProperty] private ComboBoxItem<LodStrategy> _selectedLodStrategy;
        [ObservableProperty] private bool _onlySaveVisible = false;
        [ObservableProperty] private int _numberOfLodsToGenerate;

        public SaveDialogViewModel(SceneManager sceneManager, SaveService saveService, PackFileService pfs)
        {
            _sceneManager = sceneManager;
            _saveService = saveService;
            _pfs = pfs;

            MeshStrategies = _saveService.GetGeometryStrategies().Select(x => new ComboBoxItem<GeometryStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            WsStrategies = _saveService.GetMaterialStrategies().Select(x => new ComboBoxItem<MaterialStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            LodStrategies = _saveService.GetLodStrategies().Select(x => new ComboBoxItem<LodStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
        }

        internal void Initialize(SaveSettings saveSettings)
        {
            _saveSettings = saveSettings;

            OutputPath = _saveSettings.OutputName;
            SelectedMeshStrategy= MeshStrategies.First(x => x.Value == _saveSettings.GeometryOutputType);
            SelectedWsModelStrategy= WsStrategies.First(x => x.Value == _saveSettings.MaterialOutputType);
            SelectedLodStrategy = LodStrategies.First(x => x.Value == _saveSettings.LodGenerationMethod);
            OnlySaveVisible = _saveSettings.OnlySaveVisible;
            NumberOfLodsToGenerate = _saveSettings.NumberOfLodsToGenerate;

            BuildLodOverview(_saveSettings);

            _saveSettings.IsUserInitialized = true;
        }

        partial void OnNumberOfLodsToGenerateChanged(int value) 
        {
            Guard.IsNotNull(_saveSettings);
            _saveSettings.NumberOfLodsToGenerate = value;
            _saveSettings.RefreshLodSettings();
            BuildLodOverview(_saveSettings);
        }

        void BuildLodOverview(SaveSettings saveSettings)
        {
            Guard.IsNotNull(_saveSettings);

            LodNodes.Clear();
            var lodNodesInModel = _sceneManager
                .GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel)
                .GetLodNodes();

            for (var i = 0; i < _saveSettings.NumberOfLodsToGenerate; i++)
            {
                Rmv2LodNode? lodNode = null;
                if(i < lodNodesInModel.Count)
                    lodNode = lodNodesInModel[i];
                LodNodes.Add(new LodGroupNodeViewModel(lodNode, i, saveSettings));
            }
        }
      
        public void HandleApply()
        {
            Guard.IsNotNull(_saveSettings);
            _saveSettings.OutputName = OutputPath;
            _saveSettings.OnlySaveVisible = OnlySaveVisible;
            _saveSettings.GeometryOutputType = SelectedMeshStrategy.Value;
            _saveSettings.MaterialOutputType = SelectedWsModelStrategy.Value;
            _saveSettings.LodGenerationMethod = SelectedLodStrategy.Value;
            _saveSettings.NumberOfLodsToGenerate = NumberOfLodsToGenerate;
        }

        [RelayCommand]
        void HandleBrowseLocation()
        {
            var extension = ".rigid_model_v2";
            var dialogResult = _pfs.UiProvider.DisplaySaveDialog(_pfs, [extension], out _, out var filePath);

            if (dialogResult == true)
            {
                var path = filePath!;
                if (path.Contains(extension) == false)
                    path += extension;

                OutputPath = path;
            }
        }

       public void HandleSave()
       {
           HandleApply();
       }
    }
}
