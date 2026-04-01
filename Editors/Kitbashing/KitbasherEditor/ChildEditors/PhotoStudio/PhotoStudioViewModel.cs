using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;
using Shared.Ui.BaseDialogs.ColourPickerButton;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ChildEditors.PhotoStudio
{
    public partial class PhotoStudioViewModel : ObservableObject
    {
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneRenderParametersStore _sceneRenderParameterStore;
        private readonly ArcBallCamera _arcBallCamera;

        [ObservableProperty] Vector3ViewModel _cameraPosition = new Vector3ViewModel(Vector3.Zero);
        [ObservableProperty] float _cameraYaw;
        [ObservableProperty] float _cameraPitch;
        [ObservableProperty] float _cameraZoom;
        [ObservableProperty] Vector3ViewModel _cameraLookAt = new Vector3ViewModel(Vector3.Zero);

        [ObservableProperty] float _lightIntensity;
        [ObservableProperty] ColourPickerViewModel _lightColour = new ColourPickerViewModel(Vector3.Zero);
        [ObservableProperty] float _envLightRotationY;
        [ObservableProperty] float _directLightRotationX;
        [ObservableProperty] float _directLightRotationY;

        bool _allowUpdates = false;
     

        public PhotoStudioViewModel(RenderEngineComponent renderEngineComponent, SceneRenderParametersStore sceneRenderParameterStore, ArcBallCamera arcBallCamera)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneRenderParameterStore = sceneRenderParameterStore;
            _arcBallCamera = arcBallCamera;

            // Camera
            CameraPosition = new Vector3ViewModel(arcBallCamera.Position, OnVectorChanged);
            CameraLookAt = new Vector3ViewModel(arcBallCamera.LookAt, OnVectorChanged);
            RefreshCameraValues();

            // Lighting
            LightColour = new ColourPickerViewModel(sceneRenderParameterStore.LightColour, OnVectorChanged);
            LightIntensity = sceneRenderParameterStore.LightIntensityMult;
            EnvLightRotationY = sceneRenderParameterStore.EnvLightRotationDegrees_Y;
            DirectLightRotationX = sceneRenderParameterStore.DirLightRotationDegrees_X;
            DirectLightRotationY = sceneRenderParameterStore.DirLightRotationDegrees_Y;

            _allowUpdates = true;
        }

        void OnVectorChanged(Vector3 _) => UpdateSettings();
        partial void OnCameraPitchChanged(float value) => UpdateSettings();
        partial void OnCameraYawChanged(float value) => UpdateSettings();
        partial void OnCameraZoomChanged(float value) => UpdateSettings();
        partial void OnLightIntensityChanged(float value) => UpdateSettings();
        partial void OnEnvLightRotationYChanged(float value) => UpdateSettings();
        partial void OnDirectLightRotationXChanged(float value) => UpdateSettings();
        partial void OnDirectLightRotationYChanged(float value) => UpdateSettings();

        void UpdateSettings()
        {
            if (_allowUpdates == false)
                return;

            _sceneRenderParameterStore.LightColour = LightColour.SelectedColour;
            _sceneRenderParameterStore.LightIntensityMult = LightIntensity;
            _sceneRenderParameterStore.EnvLightRotationDegrees_Y = EnvLightRotationY;
            _sceneRenderParameterStore.DirLightRotationDegrees_X = DirectLightRotationX;
            _sceneRenderParameterStore.DirLightRotationDegrees_Y = DirectLightRotationY;

            _arcBallCamera.Position = CameraPosition.GetAsVector3();
            _arcBallCamera.Yaw = CameraYaw;
            _arcBallCamera.Pitch = CameraPitch;
            _arcBallCamera.Zoom = CameraZoom;
            _arcBallCamera.LookAt = CameraLookAt.GetAsVector3();
        }

        [RelayCommand]
        private void RefreshCameraValues()
        {
            _allowUpdates = false;
            CameraPosition.Set(_arcBallCamera.Position);
            CameraYaw = _arcBallCamera.Yaw;
            CameraPitch = _arcBallCamera.Pitch;
            CameraZoom = _arcBallCamera.Zoom;
            CameraLookAt.Set(_arcBallCamera.LookAt);
            _allowUpdates = true;
        }

        [RelayCommand]
        private void SaveSettings()
        { }

        [RelayCommand]
        private void ImportSettings() { }

        [RelayCommand]
        private void TakeScreenshot()
        {
            _renderEngineComponent.SaveNextFrame(new SaveRenderImageSettings("Screenshot", true));
        }
    }
}
