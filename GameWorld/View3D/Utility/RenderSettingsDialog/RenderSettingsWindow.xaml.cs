using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Rendering;
using Shared.Ui.BaseDialogs.ColourPickerButton;

namespace GameWorld.Core.Utility.RenderSettingsDialog
{
    public partial class RenderSettingsModel : ObservableObject
    {
        [ObservableProperty] bool _useBackfaceCulling;
        [ObservableProperty] bool _useBigSceneCulling;

        [ObservableProperty] bool _showGrid;
        [ObservableProperty] ColourPickerViewModel _gridColour;

        [ObservableProperty] ColourPickerViewModel _factionColour0;
        [ObservableProperty] ColourPickerViewModel _factionColour1;
        [ObservableProperty] ColourPickerViewModel _factionColour2;

        [ObservableProperty] float _lightIntensity;
        [ObservableProperty] float _envLightRotationY;
        [ObservableProperty] float _directLightRotationX;
        [ObservableProperty] float _directLightRotationY;
    }


    public partial class RenderSettingsWindow : Window
    {
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneRenderParametersStore _sceneRenderParameterStore;
        private readonly GridComponent _gridComponent;
        private readonly RenderSettingsModel _model;

        public RenderSettingsWindow(RenderEngineComponent renderEngineComponent, SceneRenderParametersStore sceneRenderParameterStore, GridComponent gridComponent)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneRenderParameterStore = sceneRenderParameterStore;
            _gridComponent = gridComponent;
            _model = new RenderSettingsModel()
            {
                UseBackfaceCulling = renderEngineComponent.BackfaceCulling,
                UseBigSceneCulling = renderEngineComponent.LargeSceneCulling,
                
                ShowGrid = _gridComponent.ShowGrid,
                GridColour = new ColourPickerViewModel(_gridComponent.GridColur),

                LightIntensity = sceneRenderParameterStore.LightIntensityMult,
                EnvLightRotationY = sceneRenderParameterStore.EnvLightRotationDegrees_Y,
                DirectLightRotationX = sceneRenderParameterStore.DirLightRotationDegrees_X,
                DirectLightRotationY = sceneRenderParameterStore.DirLightRotationDegrees_Y,

                FactionColour0 = new ColourPickerViewModel(sceneRenderParameterStore.FactionColour0),
                FactionColour1 = new ColourPickerViewModel(sceneRenderParameterStore.FactionColour1),
                FactionColour2 = new ColourPickerViewModel(sceneRenderParameterStore.FactionColour2)
            };

            
            DataContext = _model;
            InitializeComponent();
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            _renderEngineComponent.BackfaceCulling = _model.UseBackfaceCulling;
            _renderEngineComponent.LargeSceneCulling = _model.UseBigSceneCulling;

            _gridComponent.ShowGrid = _model.ShowGrid;
            _gridComponent.GridColur = _model.GridColour.SelectedColour;

            _sceneRenderParameterStore.LightIntensityMult = _model.LightIntensity;
            _sceneRenderParameterStore.EnvLightRotationDegrees_Y = _model.EnvLightRotationY;
            _sceneRenderParameterStore.DirLightRotationDegrees_X = _model.DirectLightRotationX;
            _sceneRenderParameterStore.DirLightRotationDegrees_Y = _model.DirectLightRotationY;

            _sceneRenderParameterStore.FactionColour0 = _model.FactionColour0.SelectedColour;
            _sceneRenderParameterStore.FactionColour1 = _model.FactionColour1.SelectedColour;
            _sceneRenderParameterStore.FactionColour2 = _model.FactionColour2.SelectedColour;

            Close();
        }
    }
}
