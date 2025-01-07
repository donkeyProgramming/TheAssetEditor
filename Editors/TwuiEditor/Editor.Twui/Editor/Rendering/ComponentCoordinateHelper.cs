using Microsoft.Xna.Framework;
using Shared.GameFormats.Twui.Data;

//https://github.com/Apostolique/Apos.Gui/blob/main/Source/Dock.cs



namespace Editors.Twui.Editor.Rendering
{
    public static class ComponentCoordinateHelper
    {
        public static Vector2 GetLocalCoordinateSpace(Component component, Rectangle parentComponentSize)
        {
            var localSpace = component.Offset;

            return localSpace;
        }
    }
}


//docking = "Bottom Center"
//dock_offset = "0.00,-53.00"
//component_anchor_point = "0.50,1.00"
//offset="142.00,-81.00"
