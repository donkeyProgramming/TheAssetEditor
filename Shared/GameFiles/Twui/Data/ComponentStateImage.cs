using Microsoft.Xna.Framework;
using Shared.GameFormats.Twui.Data.DataTypes;

namespace Shared.GameFormats.Twui.Data
{
    public class ComponentStateImage
    {
        public string This { get; set; } = string.Empty;
        public string UniqueGuid { get; set; } = string.Empty;
        public string Componentimage { get; set; } = string.Empty;

        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;

        public DockingVertical DockingVertical { get; set; } = DockingVertical.None;
        public DockingHorizontal DockingHorizontal { get; set; } = DockingHorizontal.None;
        public Vector2 Offset { get; set; } = Vector2.Zero;
        public Vector2 Dock_offset { get; set; } = new Vector2(0.5f, 0.5f); // This is the same as anchorpoint in a normal component
        public Vector4 Margin { get; set; } = Vector4.Zero;

        public string Colour { get; set; } = string.Empty;
    }
}


