using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameWorld.Core.Components.Rendering;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.ColourPickerButton;
using Shared.Ui.BaseDialogs.MathViews;
using Shared.Ui.BaseDialogs.StandardDialog;

namespace Editors.KitbasherEditor.ChildEditors.PhotoStudio
{
    public partial class PhotoStudioViewModel : ObservableObject
    {
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneRenderParametersStore _sceneRenderParameterStore;
        private readonly ArcBallCamera _arcBallCamera;
        private readonly IStandardDialogs _standardDialogs;
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
        [ObservableProperty] bool _doubleImageResolution = true;

        bool _allowUpdates = false;
     

        public PhotoStudioViewModel(RenderEngineComponent renderEngineComponent, SceneRenderParametersStore sceneRenderParameterStore, ArcBallCamera arcBallCamera, IStandardDialogs standardDialogs)
        {
            _renderEngineComponent = renderEngineComponent;
            _sceneRenderParameterStore = sceneRenderParameterStore;
            _arcBallCamera = arcBallCamera;
            _standardDialogs = standardDialogs;

            // Camera
            CameraPosition = new Vector3ViewModel(arcBallCamera.Position, OnCamerVectorChanged);
            CameraLookAt = new Vector3ViewModel(arcBallCamera.LookAt, OnCamerVectorChanged);
            RefreshCameraValues();

            // Lighting
            LightColour = new ColourPickerViewModel(sceneRenderParameterStore.LightColour, OnLightVectorChanged);
            LightIntensity = sceneRenderParameterStore.LightIntensityMult;
            EnvLightRotationY = sceneRenderParameterStore.EnvLightRotationDegrees_Y;
            DirectLightRotationX = sceneRenderParameterStore.DirLightRotationDegrees_X;
            DirectLightRotationY = sceneRenderParameterStore.DirLightRotationDegrees_Y;

            _allowUpdates = true;
        }

        void OnCamerVectorChanged(Vector3 _) => UpdateCamera();
        void OnLightVectorChanged(Vector3 _) => UpdateLight();
        partial void OnCameraPitchChanged(float value) => UpdateCamera();
        partial void OnCameraYawChanged(float value) => UpdateCamera();
        partial void OnCameraZoomChanged(float value) => UpdateCamera();
        partial void OnLightIntensityChanged(float value) => UpdateLight();
        partial void OnEnvLightRotationYChanged(float value) => UpdateLight();
        partial void OnDirectLightRotationXChanged(float value) => UpdateLight();
        partial void OnDirectLightRotationYChanged(float value) => UpdateLight();

        void UpdateLight()
        {
            if (_allowUpdates == false)
                return;

            _sceneRenderParameterStore.LightColour = LightColour.SelectedColour;
            _sceneRenderParameterStore.LightIntensityMult = LightIntensity;
            _sceneRenderParameterStore.EnvLightRotationDegrees_Y = EnvLightRotationY;
            _sceneRenderParameterStore.DirLightRotationDegrees_X = DirectLightRotationX;
            _sceneRenderParameterStore.DirLightRotationDegrees_Y = DirectLightRotationY;
        }

        void UpdateCamera()
        {
            if (_allowUpdates == false)
                return;

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
        {
            var camPos = CameraPosition.GetAsVector3();
            var camLook = CameraLookAt.GetAsVector3();
            var lightCol = LightColour.SelectedColour;

            var settings = new PhotoStuidoSettings
            {
                CameraPositionX = camPos.X,
                CameraPositionY = camPos.Y,
                CameraPositionZ = camPos.Z,
                CameraYaw = CameraYaw,
                CameraPitch = CameraPitch,
                CameraZoom = CameraZoom,
                CameraLookAtX = camLook.X,
                CameraLookAtY = camLook.Y,
                CameraLookAtZ = camLook.Z,
                LightIntensity = LightIntensity,
                LightColourX = lightCol.X,
                LightColourY = lightCol.Y,
                LightColourZ = lightCol.Z,
                EnvLightRotationY = EnvLightRotationY,
                DirectLightRotationX = DirectLightRotationX,
                DirectLightRotationY = DirectLightRotationY,
            };

            var dlg = new SaveFileDialog()
            {
                Filter = "Json files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "PhotoStudioSettings.json"
            };

            var result = dlg.ShowDialog();
            if (result != true)
                return;

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);
        }

        [RelayCommand]
        private void ImportSettings()
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Json files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false
            };

            var result = dlg.ShowDialog();
            if (result != true || string.IsNullOrEmpty(dlg.FileName))
                return;

            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var settings = JsonSerializer.Deserialize<PhotoStuidoSettings>(json);
                if (settings == null)
                    return;

                CameraPosition.Set(settings.CameraPositionX, settings.CameraPositionY, settings.CameraPositionZ);
                CameraYaw = settings.CameraYaw;
                CameraPitch = settings.CameraPitch;
                CameraZoom = settings.CameraZoom;
                CameraLookAt.Set(settings.CameraLookAtX, settings.CameraLookAtY, settings.CameraLookAtZ);

                var importedLight = new Vector3(settings.LightColourX, settings.LightColourY, settings.LightColourZ);
                LightColour = new ColourPickerViewModel(importedLight, OnLightVectorChanged);
                LightIntensity = settings.LightIntensity;
                EnvLightRotationY = settings.EnvLightRotationY;
                DirectLightRotationX = settings.DirectLightRotationX;
                DirectLightRotationY = settings.DirectLightRotationY;

                UpdateLight();
                UpdateCamera();
            }
            catch(Exception ex)
            {
                _standardDialogs.ShowExceptionWindow(ex);
            }
        }

        [RelayCommand]
        private void TakeScreenshot()
        {
            var imageScale = 1.0f;
            if (DoubleImageResolution)
                imageScale = 2.0f;

            var outputFolder = Path.Combine(DirectoryHelper.ApplicationDirectory, "PhotoStudio", "Screenshots");
            _renderEngineComponent.SaveNextFrame(new SaveRenderImageSettings("Screenshot", true, false, imageScale, outputFolder));
        }
    }
}
