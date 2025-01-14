using Microsoft.Xna.Framework;
using Shared.GameFormats.Twui.Data;
using Shared.GameFormats.Twui.Data.DataTypes;

//https://github.com/Apostolique/Apos.Gui/blob/main/Source/Dock.cs



namespace Editors.Twui.Editor.Rendering
{
    public static class ComponentCoordinateHelper
    {
        public static Rectangle GetLocalCoordinateSpace(Component component, Rectangle parentComponentRect, int depth)
        {
            var currentStateId = component.Currentstate;
            var currentState = component.States.FirstOrDefault(x => x.UniqueGuid == currentStateId);
            if (currentState == null)
            {
                return parentComponentRect; // Happens sometimes when using tempplates. Solve later
                //throw new Exception($"Current state {currentStateId} not found in {component.Name}[{component.Id}]");
            }

            if (component.Name == "holder_grudge_cycles")
            { 
            }

            var localSpace = new Vector2(parentComponentRect.X, parentComponentRect.Y);
            var width = (int)currentState.Width;
            var height = (int)currentState.Height;





            if (component.DockingHorizontal != DockingHorizontal.None || component.DockingVertical != DockingVertical.None)
            {
                switch (component.DockingHorizontal)
                {
                    case DockingHorizontal.Left:
                        localSpace.X = parentComponentRect.X;
                        break;

                    case DockingHorizontal.Right:
                        localSpace.X = parentComponentRect.Right - width;
                        break;

                    case DockingHorizontal.Center:
                        localSpace.X = parentComponentRect.Left + (parentComponentRect.Width * 0.5f);
                        break;
                }

                switch (component.DockingVertical)
                {
                    case DockingVertical.Top:
                        localSpace.Y = parentComponentRect.Top;
                        break;

                    case DockingVertical.Bottom:
                        localSpace.Y = parentComponentRect.Bottom - height;
                        break;

                    case DockingVertical.Center:
                        localSpace.Y = parentComponentRect.Top +(parentComponentRect.Height * 0.5f);
                        break;
                }

            
                localSpace += component.Dock_offset;

               
                var anchorOffset = new Vector2(width, height) * component.Component_anchor_point;
                localSpace -= anchorOffset;
            }


            return new Rectangle((int)localSpace.X, (int)localSpace.Y, width, height);
        }


    }
}


//docking = "Bottom Center"
//dock_offset = "0.00,-53.00"
//component_anchor_point = "0.50,1.00"
//offset="142.00,-81.00"


// From state 
//width = "1336"
//    height = "821"


// image has offset
// Image has margin
// Image has dockpoint
// Image has dock_offset
