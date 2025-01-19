using System.Diagnostics;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Twui.Data.DataTypes;

namespace Shared.GameFormats.Twui.Data
{

    // If values are not set, null is probably better? 
    // Stuff like Dimensions are not always set, and 0,0 is very different then not set
    [DebuggerDisplay("{Name} - {This}")]
    public class Component 
    {
        public string Name { get; set; } = string.Empty;
        public string This { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool PartOfTemplate { get; set; } = false;
        public string Uniqueguid_in_template { get; set; } = string.Empty;
        public string Template_id{ get; set; } = string.Empty;
        public string Uniqueguid { get; set; } = string.Empty;
        public Vector2 Dimensions { get; set; } = new(0, 0);
        public DockingVertical DockingVertical { get; set; } = DockingVertical.None;
        public DockingHorizontal DockingHorizontal { get; set; } = DockingHorizontal.None;
        public bool Tooltips_localised { get; set; } = false;
        public Vector2 Offset { get; set; } = new(0, 0);
        public float Priority { get; set; } = 100;
        public Vector2 Component_anchor_point { get; set; } = new(0, 0);
        public Vector2 Dock_offset { get; set; } = new(0, 0);
        public string Defaultstate { get; set; } = string.Empty;
        public string Currentstate { get; set; } = string.Empty;
        public bool Allowhorizontalresize { get; set; } = false;
        public bool Allowverticalresize { get; set; } = false;

        public List<ComponentImage> ComponentImages { get; set; } = [];
        public List<ComponentState> States { get; set; } = [];

        //LayoutEngine
    }
}


