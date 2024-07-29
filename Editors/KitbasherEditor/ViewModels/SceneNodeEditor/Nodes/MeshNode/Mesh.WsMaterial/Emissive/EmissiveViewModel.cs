using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
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

        public EmissiveViewModel(EmissiveCapability emissiveCapability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _emissiveCapability = emissiveCapability;

            _emissiveTexture = new ShaderTextureViewModel(emissiveCapability.Emissive, packFileService, uiCommandFactory, resourceLibrary);
            _emissiveDistortionTexture = new ShaderTextureViewModel(emissiveCapability.EmissiveDistortion, packFileService, uiCommandFactory, resourceLibrary);

            _emissiveDirection = new Vector2ViewModel(emissiveCapability.EmissiveDirection, OnEmissiveDirectionChanged);
            _emissiveDistortStrength = emissiveCapability.EmissiveDistortStrength;
            _emissiveFresnelStrength = emissiveCapability.EmissiveFresnelStrength;

            _emissiveTint = new ColourPickerViewModel(emissiveCapability.EmissiveTint, OnEmissiveTintChanged);

            _gradient0 = new ColourPickerViewModel(GetGradientColour(emissiveCapability.Gradient[0]), OnGradientColour0Changed);
            _gradient1 = new ColourPickerViewModel(GetGradientColour(emissiveCapability.Gradient[1]), OnGradientColour1Changed);
            _gradient2 = new ColourPickerViewModel(GetGradientColour(emissiveCapability.Gradient[2]), OnGradientColour2Changed);
            _gradient3 = new ColourPickerViewModel(GetGradientColour(emissiveCapability.Gradient[3]), OnGradientColour3Changed);

            _gradientTime0 = GetGradientTime(emissiveCapability.Gradient[0]);
            _gradientTime1 = GetGradientTime(emissiveCapability.Gradient[1]);
            _gradientTime2 = GetGradientTime(emissiveCapability.Gradient[2]);
            _gradientTime3 = GetGradientTime(emissiveCapability.Gradient[3]);

            _emissiveSpeed = emissiveCapability.EmissiveSpeed;
            _emissivePulseSpeed = emissiveCapability.EmissivePulseSpeed;
            _emissivePulseStrength = emissiveCapability.EmissivePulseStrength;
            _emissiveStrength = emissiveCapability.EmissiveStrength;

            _emissiveTiling = new Vector2ViewModel(emissiveCapability.EmissiveTiling, OnEmissiveTilingChanged);
        }

        void OnEmissiveDirectionChanged(Vector2 value) => _emissiveCapability.EmissiveDirection = value;
        void OnEmissiveTintChanged(Vector3 value) => _emissiveCapability.EmissiveTint = value;
        void OnEmissiveTilingChanged(Vector2 value) => _emissiveCapability.EmissiveTiling = value;
        void OnGradientColour0Changed(Vector3 color) => _emissiveCapability.Gradient[0] = new Vector4(color, _emissiveCapability.Gradient[0].W);
        void OnGradientColour1Changed(Vector3 color) => _emissiveCapability.Gradient[1] = new Vector4(color, _emissiveCapability.Gradient[1].W);
        void OnGradientColour2Changed(Vector3 color) => _emissiveCapability.Gradient[2] = new Vector4(color, _emissiveCapability.Gradient[2].W);
        void OnGradientColour3Changed(Vector3 color) => _emissiveCapability.Gradient[3] = new Vector4(color, _emissiveCapability.Gradient[3].W);

        static Vector3 GetGradientColour(Vector4 gradient) => new(gradient.X, gradient.Y, gradient.Z);
        static float GetGradientTime(Vector4 gradient) => gradient.W;
    }
}
