using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;
using GameWorld.Core.Utility.UserInterface;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.ColourPickerButton;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.Emissive
{
    public partial class EmissiveViewModel : ObservableObject
    {
        private readonly EmissiveCapability _emissiveCapability;

        [ObservableProperty] ShaderTextureViewModel _emissiveTexture;
        [ObservableProperty] ShaderTextureViewModel _emissiveDistortionTexture;
        [ObservableProperty] Vector2ViewModel _emissiveDirection;
        [ObservableProperty] float _emissiveDistortStrength;
        [ObservableProperty] float _emissiveFresnelStrength;
        [ObservableProperty] ColourPickerViewModel _emissiveTint;
        [ObservableProperty] ColourPickerViewModel _gradient0;
        [ObservableProperty] ColourPickerViewModel _gradient1;
        [ObservableProperty] ColourPickerViewModel _gradient2;
        [ObservableProperty] ColourPickerViewModel _gradient3;
        [ObservableProperty] float _gradientTime0;
        [ObservableProperty] float _gradientTime1;
        [ObservableProperty] float _gradientTime2;
        [ObservableProperty] float _gradientTime3;

        [ObservableProperty] float _emissiveSpeed;
        [ObservableProperty] float _emissivePulseSpeed;
        [ObservableProperty] float _emissivePulseStrength;
        [ObservableProperty] float _emissiveStrength;
        [ObservableProperty] Vector2ViewModel _emissiveTiling;

        public EmissiveViewModel(EmissiveCapability emissiveCapability, IUiCommandFactory uiCommandFactory, IPackFileService packFileService, IScopedResourceLibrary resourceLibrary, IStandardDialogs packFileUiProvider)
        {
            _emissiveCapability = emissiveCapability;

            _emissiveTexture = new ShaderTextureViewModel(emissiveCapability.Emissive, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);
            _emissiveDistortionTexture = new ShaderTextureViewModel(emissiveCapability.EmissiveDistortion, packFileService, uiCommandFactory, resourceLibrary, packFileUiProvider);

            _emissiveDirection = new Vector2ViewModel(emissiveCapability.EmissiveDirection, OnEmissiveDirectionChanged);
            _emissiveDistortStrength = emissiveCapability.EmissiveDistortStrength;
            _emissiveFresnelStrength = emissiveCapability.EmissiveFresnelStrength;

            _emissiveTint = new ColourPickerViewModel(emissiveCapability.EmissiveTint, OnEmissiveTintChanged);

            _gradient0 = new ColourPickerViewModel(emissiveCapability.GradientColours[0], OnGradientColour0Changed);
            _gradient1 = new ColourPickerViewModel(emissiveCapability.GradientColours[1], OnGradientColour1Changed);
            _gradient2 = new ColourPickerViewModel(emissiveCapability.GradientColours[2], OnGradientColour2Changed);
            _gradient3 = new ColourPickerViewModel(emissiveCapability.GradientColours[3], OnGradientColour3Changed);

            _gradientTime0 = emissiveCapability.GradientTimes[0];
            _gradientTime1 = emissiveCapability.GradientTimes[1];
            _gradientTime2 = emissiveCapability.GradientTimes[2];
            _gradientTime3 = emissiveCapability.GradientTimes[3];

            _emissiveSpeed = emissiveCapability.EmissiveSpeed;
            _emissivePulseSpeed = emissiveCapability.EmissivePulseSpeed;
            _emissivePulseStrength = emissiveCapability.EmissivePulseStrength;
            _emissiveStrength = emissiveCapability.EmissiveStrength;

            _emissiveTiling = new Vector2ViewModel(emissiveCapability.EmissiveTiling, OnEmissiveTilingChanged);
        }

        partial void OnEmissiveDistortStrengthChanged(float value) => _emissiveCapability.EmissiveDistortStrength = value;
        partial void OnEmissiveSpeedChanged(float value) => _emissiveCapability.EmissiveSpeed = value;
        partial void OnEmissivePulseStrengthChanged(float value) => _emissiveCapability.EmissivePulseStrength = value;
        partial void OnEmissivePulseSpeedChanged(float value) => _emissiveCapability.EmissivePulseSpeed = value;
        void OnEmissiveDirectionChanged(Vector2 value) => _emissiveCapability.EmissiveDirection = value;
        void OnEmissiveTintChanged(Vector3 value) => _emissiveCapability.EmissiveTint = value;
        partial void OnEmissiveStrengthChanged(float value) => _emissiveCapability.EmissiveStrength = value;
        partial void OnEmissiveFresnelStrengthChanged(float value) => _emissiveCapability.EmissiveFresnelStrength = value;

        void OnEmissiveTilingChanged(Vector2 value) => _emissiveCapability.EmissiveTiling = value;
        void OnGradientColour0Changed(Vector3 color) => _emissiveCapability.GradientColours[0] = color;
        void OnGradientColour1Changed(Vector3 color) => _emissiveCapability.GradientColours[1] = color;
        void OnGradientColour2Changed(Vector3 color) => _emissiveCapability.GradientColours[2] = color;
        void OnGradientColour3Changed(Vector3 color) => _emissiveCapability.GradientColours[3] = color;

        partial void OnGradientTime0Changed(float value) => _emissiveCapability.GradientTimes[0] = value;
        partial void OnGradientTime1Changed(float value) => _emissiveCapability.GradientTimes[1] = value;
        partial void OnGradientTime2Changed(float value) => _emissiveCapability.GradientTimes[2] = value;
        partial void OnGradientTime3Changed(float value) => _emissiveCapability.GradientTimes[3] = value;

    }
}
