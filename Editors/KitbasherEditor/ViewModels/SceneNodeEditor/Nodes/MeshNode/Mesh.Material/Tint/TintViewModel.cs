using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using Shared.Ui.BaseDialogs.ColourPickerButton;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.Emissive
{
    public partial class TintViewModel : ObservableObject
    {
        private readonly TintCapability _tintCapability;

        [ObservableProperty] bool _applyCapability;

        [ObservableProperty] Vector4ViewModel _tintMask;
        [ObservableProperty] ColourPickerViewModel _tintColour;
        [ObservableProperty] float _tintVariation;

        [ObservableProperty] bool _useFactionColours;
        [ObservableProperty] ColourPickerViewModel _factionColour0;
        [ObservableProperty] ColourPickerViewModel _factionColour1;
        [ObservableProperty] ColourPickerViewModel _factionColour2;
        [ObservableProperty] Vector4ViewModel _factionColour3Mask;

        public TintViewModel(TintCapability tintCapability)
        {
            _tintCapability = tintCapability;

            _applyCapability = _tintCapability.ApplyCapability;

            _tintMask = new Vector4ViewModel(_tintCapability.DiffuseTintMask, (c) => _tintCapability.DiffuseTintMask = c);
            _tintColour = new ColourPickerViewModel(_tintCapability.DiffuseTintColour, (c) => _tintCapability.DiffuseTintColour = c);
            _tintVariation = _tintCapability.DiffuseTintVariation;

            _useFactionColours = _tintCapability.UseFactionColours;
            _factionColour0 = new ColourPickerViewModel(_tintCapability.FactionColours[0], (c) => _tintCapability.FactionColours[0] = c);
            _factionColour1 = new ColourPickerViewModel(_tintCapability.FactionColours[1], (c) => _tintCapability.FactionColours[1] = c);
            _factionColour2 = new ColourPickerViewModel(_tintCapability.FactionColours[2], (c) => _tintCapability.FactionColours[2] = c);
            _factionColour3Mask = new Vector4ViewModel(_tintCapability.Faction3Mask, (c) => _tintCapability.Faction3Mask = c);
        }

        partial void OnTintVariationChanged(float value) => _tintCapability.DiffuseTintVariation = value;
        partial void OnUseFactionColoursChanged(bool value) => _tintCapability.UseFactionColours = value;
        partial void OnApplyCapabilityChanged(bool value) => _tintCapability.ApplyCapability = value;
    }
}
