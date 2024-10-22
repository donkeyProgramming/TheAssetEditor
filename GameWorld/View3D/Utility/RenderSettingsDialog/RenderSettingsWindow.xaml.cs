using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Components.Rendering;
using Shared.Ui.BaseDialogs.ColourPickerButton;

namespace GameWorld.Core.Utility.RenderSettingsDialog
{
    public partial class RenderSettingsModel : ObservableObject
    {
        [ObservableProperty] bool _useBackfaceCulling;
        [ObservableProperty] bool _useBigSceneCulling;


        [ObservableProperty] ColourPickerViewModel _factionColour0;
        [ObservableProperty] ColourPickerViewModel _factionColour1;
        [ObservableProperty] ColourPickerViewModel _factionColour2;

        [ObservableProperty] float _lightIntensity;
        [ObservableProperty] float _envLightRotationY;
        [ObservableProperty] float _directLightRotationX;
        [ObservableProperty] float _directLightRotationY;
    }

    /// <summary>
    /// Interaction logic for RenderSettingsWindow.xaml
    /// </summary>
    public partial class RenderSettingsWindow : Window
    {
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneRenderParametersStore _sceneRenderParameterStore;
        private readonly RenderSettingsModel _model;

        public RenderSettingsWindow(RenderEngineComponent renderEngineComponent, SceneRenderParametersStore sceneRenderParameterStore)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneRenderParameterStore = sceneRenderParameterStore;

            _model = new RenderSettingsModel()
            {
                UseBackfaceCulling = renderEngineComponent.BackfaceCulling,
                UseBigSceneCulling = renderEngineComponent.LargeSceneCulling,

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
