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
        private readonly GameWorld.Core.Services.SceneSaving.SaveService _saveService;
        private readonly IPackFileService _pfs;
        private readonly IPackFileUiProvider _packFileUiProvider;
        private GeometrySaveSettings? _saveSettings;

        [ObservableProperty] ObservableCollection<LodGroupNodeViewModel> _lodNodes = [];
        [ObservableProperty] List<ComboBoxItem<GeometryStrategy>> _meshStrategies;
        [ObservableProperty] List<ComboBoxItem<MaterialStrategy>> _wsStrategies;
        [ObservableProperty] List<ComboBoxItem<LodStrategy>> _lodStrategies;
        [ObservableProperty] List<int> _possibleLodNumbers  = [1,4,5];

        [ObservableProperty] string _outputPath;
        [ObservableProperty] ComboBoxItem<GeometryStrategy> _selectedMeshStrategy;
        [ObservableProperty] ComboBoxItem<MaterialStrategy> _selectedWsModelStrategy;
        [ObservableProperty] ComboBoxItem<LodStrategy> _selectedLodStrategy;
        [ObservableProperty] bool _onlySaveVisible = false;
        [ObservableProperty] int _numberOfLodsToGenerate;

        public SaveDialogViewModel(SceneManager sceneManager, GameWorld.Core.Services.SceneSaving.SaveService saveService, IPackFileService pfs, IPackFileUiProvider packFileUiProvider)
        {
            _sceneManager = sceneManager;
            _saveService = saveService;
            _pfs = pfs;
            _packFileUiProvider = packFileUiProvider;
            MeshStrategies = _saveService.GetGeometryStrategies().Select(x => new ComboBoxItem<GeometryStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            WsStrategies = _saveService.GetMaterialStrategies().Select(x => new ComboBoxItem<MaterialStrategy>(x.StrategyId, x.Name, x.Description)).ToList();
            LodStrategies = _saveService.GetLodStrategies().Select(x => new ComboBoxItem<LodStrategy>(x.StrategyId, x.Name, x.Description)).ToList();

            OutputPath = "";
            SelectedMeshStrategy = MeshStrategies.First();
            SelectedWsModelStrategy = WsStrategies.First();
            SelectedLodStrategy = LodStrategies.First();
        }

        internal void Initialize(GeometrySaveSettings saveSettings)
        {
            _saveSettings = saveSettings;
            _saveSettings.IsUserInitialized = true;

            OutputPath = _saveSettings.OutputName;
            SelectedMeshStrategy= MeshStrategies.First(x => x.Value == _saveSettings.GeometryOutputType);
            SelectedWsModelStrategy= WsStrategies.First(x => x.Value == _saveSettings.MaterialOutputType);
            SelectedLodStrategy = LodStrategies.First(x => x.Value == _saveSettings.LodGenerationMethod);
            OnlySaveVisible = _saveSettings.OnlySaveVisible;
            NumberOfLodsToGenerate = _saveSettings.NumberOfLodsToGenerate;

            BuildLodOverview(_saveSettings);
        }

        partial void OnNumberOfLodsToGenerateChanged(int value) 
        {
            Guard.IsNotNull(_saveSettings);
            _saveSettings.NumberOfLodsToGenerate = value;
            _saveSettings.RefreshLodSettings();
            BuildLodOverview(_saveSettings);
        }

        void BuildLodOverview(GeometrySaveSettings saveSettings)
        {
            LodNodes.Clear();
            var lodNodesInModel = _sceneManager
                .GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel)
                .GetLodNodes();

            for (var i = 0; i < saveSettings.NumberOfLodsToGenerate; i++)
            {
                Rmv2LodNode? lodNode = null;
                if(i < lodNodesInModel.Count)
                    lodNode = lodNodesInModel[i];
                LodNodes.Add(new LodGroupNodeViewModel(lodNode, i, saveSettings));
            }
        }
      
        public void ApplySettings()
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
            var dialogResult = _packFileUiProvider.DisplaySaveDialog(_pfs, [extension]);

            if (dialogResult.Result == true)
            {
                var path = dialogResult.SelectedFilePath!;
                if (path.Contains(extension) == false)
                    path += extension;

                OutputPath = path;
            }
        }
    }
}
