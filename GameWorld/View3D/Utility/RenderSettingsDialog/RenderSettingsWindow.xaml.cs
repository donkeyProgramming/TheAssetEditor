using System.Windows;
using GameWorld.Core.Components.Grid;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Shared.Ui.BaseDialogs.ColourPickerButton;

namespace GameWorld.Core.Utility.RenderSettingsDialog
{
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
                GridColour = new ColourPickerViewModel(_gridComponent.GridColur, ColourChanged),

                LightIntensity = sceneRenderParameterStore.LightIntensityMult,
                LightColour = new ColourPickerViewModel(sceneRenderParameterStore.LightColour, ColourChanged),

                EnvLightRotationY = sceneRenderParameterStore.EnvLightRotationDegrees_Y,
                DirectLightRotationX = sceneRenderParameterStore.DirLightRotationDegrees_X,
                DirectLightRotationY = sceneRenderParameterStore.DirLightRotationDegrees_Y,

                FactionColour0 = new ColourPickerViewModel(sceneRenderParameterStore.FactionColour0, ColourChanged),
                FactionColour1 = new ColourPickerViewModel(sceneRenderParameterStore.FactionColour1, ColourChanged),
                FactionColour2 = new ColourPickerViewModel(sceneRenderParameterStore.FactionColour2, ColourChanged)
            };

            _model.PropertyChanged += OnModelChanged;

            DataContext = _model;
            InitializeComponent();
        }

        private void Window_OnContentRendered(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private void ColourChanged(Vector3 _)
        {
            OnModelChanged(null, null);
        }

        private void OnModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _renderEngineComponent.BackfaceCulling = _model.UseBackfaceCulling;
            _renderEngineComponent.LargeSceneCulling = _model.UseBigSceneCulling;

            _gridComponent.ShowGrid = _model.ShowGrid;
            _gridComponent.GridColur = _model.GridColour.SelectedColour;

            _sceneRenderParameterStore.LightIntensityMult = _model.LightIntensity;
            _sceneRenderParameterStore.LightColour = _model.LightColour.SelectedColour;

            _sceneRenderParameterStore.EnvLightRotationDegrees_Y = _model.EnvLightRotationY;
            _sceneRenderParameterStore.DirLightRotationDegrees_X = _model.DirectLightRotationX;
            _sceneRenderParameterStore.DirLightRotationDegrees_Y = _model.DirectLightRotationY;

            _sceneRenderParameterStore.FactionColour0 = _model.FactionColour0.SelectedColour;
            _sceneRenderParameterStore.FactionColour1 = _model.FactionColour1.SelectedColour;
            _sceneRenderParameterStore.FactionColour2 = _model.FactionColour2.SelectedColour;
        }

    }
}
