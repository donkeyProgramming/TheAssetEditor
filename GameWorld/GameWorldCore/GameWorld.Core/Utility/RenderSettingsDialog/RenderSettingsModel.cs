using CommunityToolkit.Mvvm.ComponentModel;
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
        [ObservableProperty] ColourPickerViewModel _lightColour;
        [ObservableProperty] float _envLightRotationY;
        [ObservableProperty] float _directLightRotationX;
        [ObservableProperty] float _directLightRotationY;

     
    }
}
