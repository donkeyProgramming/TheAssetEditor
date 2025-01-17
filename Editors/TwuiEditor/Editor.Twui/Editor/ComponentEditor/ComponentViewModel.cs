using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.Twui.Editor.ComponentEditor
{
    public partial class ImageMetricInfo : ObservableObject
    {
        [ObservableProperty] public partial string ImagePath { get; set; }

        [ObservableProperty] public partial Vector2ViewModel Offset { get; set; }
        [ObservableProperty] public partial Vector2ViewModel DockOffset { get; set; }
        [ObservableProperty] public partial Vector2ViewModel Size { get; set; }
        [ObservableProperty] public partial string DockPoint { get; set; }
        [ObservableProperty] public partial bool Tile { get; set; }
    }


    public partial class ComponentViewModel : ObservableObject
    {
        [ObservableProperty] public partial string Name { get; set; }
        [ObservableProperty] public partial string Id { get; set; }
        [ObservableProperty] public partial bool IsChildComponent { get; set; }
        [ObservableProperty] public partial Vector2ViewModel Offset { get; set; } = new Vector2ViewModel(0,0);
        [ObservableProperty] public partial string Docking { get; set; }
        [ObservableProperty] public partial Vector2ViewModel Docking_Offset { get; set; } = new Vector2ViewModel(0, 0);
        [ObservableProperty] public partial Vector2ViewModel Anchor_point { get; set; } = new Vector2ViewModel(0, 0);


        [ObservableProperty] public partial Vector2ViewModel CurrentState_Size { get; set; } = new Vector2ViewModel(0, 0);
        [ObservableProperty] public partial List<ImageMetricInfo> CurrentStae_ImageInfo { get; set; } = new List<ImageMetricInfo>();


        [ObservableProperty] public partial string TestString { get; set; } = "SomeTest";
        [ObservableProperty] public partial bool TestBool { get; set; } = true;
        [ObservableProperty] public partial float TestFloat { get; set; } = 1.23f;
        [ObservableProperty] public partial Vector2ViewModel TextVector2 { get; set; } = new Vector2ViewModel(11,21.3f);


        public ComponentViewModel(TwuiComponent selectedComponent)
        {
           // _selectedComponent = selectedComponent;
            TextVector2 = new Vector2ViewModel(11, 21.3f, OnChanged);
        }

        private void OnChanged(Vector2 vector)
        {
            //throw new NotImplementedException();
        }

        partial void OnTestStringChanged(string value)
        {
            //throw new NotImplementedException();
        }

        partial void OnTestBoolChanged(bool value)
        {
           // throw new NotImplementedException();
        }


    }
}




// Name 
// Offset
// Docking
// Docking offset
// Size
// Images
// Texts
// IsChildComponent

// State info
