using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Twui.Data.DataTypes;

namespace Editors.Twui.Editor
{


    public partial class TwuiContext : ObservableObject
    {
        [ObservableProperty] public partial string FileName { get; set; } = "";
        [ObservableProperty] public partial string? SkinOverride { get; set; }

        [ObservableProperty] public partial ObservableCollection<TwuiComponent> Componenets { get; set; } = [];
    }


    [DebuggerDisplay("{Name} - {Id}")]
    public partial class TwuiComponent : ObservableObject
    {
        [ObservableProperty] public partial string Name { get; set; } = "";
        [ObservableProperty] public partial string Id { get; set; } = "";
        [ObservableProperty] public partial float Priority { get; set; }

        [ObservableProperty] public partial bool IsPartOfTemplate { get; set; }
        [ObservableProperty] public partial string? TemplateName { get; set; }

        [ObservableProperty] public partial TwuiComponentState? CurrentState { get; set; }
        [ObservableProperty] public partial ObservableCollection<TwuiComponentState> States { get; set; } = [];


        [ObservableProperty] public partial TwuiLocation Location { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<TwuiComponent> Children { get; set; }


        [ObservableProperty] public partial bool IsSelected { get; set; } = false;
        [ObservableProperty] public partial bool ShowInPreviewRenderer { get; set; } = true;

    }

    public partial class TwuiLocation : ObservableObject
    {
        [ObservableProperty] public partial DockingVertical DockingVertical { get; set; } = DockingVertical.None;
        [ObservableProperty] public partial DockingHorizontal DockingHorizontal { get; set; } = DockingHorizontal.None;
        [ObservableProperty] public partial Vector2 Offset { get; set; } = new(0, 0);
        [ObservableProperty] public partial Vector2 Component_anchor_point { get; set; } = new(0, 0);
        [ObservableProperty] public partial Vector2 Dock_offset { get; set; } = new(0, 0);
        [ObservableProperty] public partial Vector2 Dimensions { get; set; } = new(0, 0);

        internal TwuiLocation Clone()
        {
            return new TwuiLocation()
            {
                Component_anchor_point = Component_anchor_point,
                Dock_offset = Dock_offset,
                Dimensions = Dimensions,
                Offset = Offset,
                DockingHorizontal = DockingHorizontal,
                DockingVertical = DockingVertical
            };
        }
    }

    [DebuggerDisplay("{Name} - {Id}")]
    public partial class TwuiComponentState : ObservableObject
    {
        [ObservableProperty] public partial string Name { get; set; } = "";
        [ObservableProperty] public partial string Id { get; set; } = "";

        [ObservableProperty] public partial float Width { get; set; } = 0;
        [ObservableProperty] public partial float Height { get; set; } = 0;

        [ObservableProperty] public partial ObservableCollection<TwuiComponentImage> ImageList { get; set; } = [];

    }

    [DebuggerDisplay("{Path}")]
    public partial class TwuiComponentImage : ObservableObject
    {
        [ObservableProperty] public partial string? Path { get; set; } = "";
        [ObservableProperty] public partial string Id { get; set; } = "";
        [ObservableProperty] public partial TwuiLocation Location { get; set; } = new();
    }
}
