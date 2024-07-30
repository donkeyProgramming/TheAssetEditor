using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;

namespace Shared.Ui.BaseDialogs.ColourPickerButton
{
    public partial class ColourPickerViewModel : ObservableObject
    {
        private readonly Action<Vector3>? _onColourChangedCallback;
        
        public Vector3 SelectedColour { get; private set; }
        [ObservableProperty] System.Windows.Media.Color _pickedColor;

        public ColourPickerViewModel(Vector3 colour, Action<Vector3>? OnColourChangedCallback = null)
        {
            SelectedColour = colour;
            _onColourChangedCallback = OnColourChangedCallback;
            _pickedColor = System.Windows.Media.Color.FromRgb((byte)(colour.X * 255f), (byte)(colour.Y * 255f), (byte)(colour.Z * 255f));
        }

        [RelayCommand]
        public void OnHandleColourChanged()
        {
            SelectedColour = new Vector3(PickedColor.R / 256f, PickedColor.G / 256f, PickedColor.B / 256f);
            _onColourChangedCallback?.Invoke(SelectedColour);
        }
    }
}
