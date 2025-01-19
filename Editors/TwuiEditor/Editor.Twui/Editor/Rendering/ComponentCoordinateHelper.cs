using Microsoft.Xna.Framework;
using Shared.GameFormats.Twui.Data.DataTypes;


//https://github.com/Apostolique/Apos.Gui/blob/main/Source/Dock.cs



namespace Editors.Twui.Editor.Rendering
{
    public static class ComponentCoordinateHelper
    {
        public static Rectangle GetComponentStateLocalCoordinateSpace(TwuiComponent component, Rectangle parentComponentRect)
        {
            var currentState = component.CurrentState;
            if (currentState == null)
            {
                return parentComponentRect; // Happens sometimes when using tempplates. Solve later
                //throw new Exception($"Current state {currentStateId} not found in {component.Name}[{component.Id}]");
            }

            if (component.Name == "round_small_button")
            {
                //offset = new Vector2(-230, -15);
            }

            if (component.Name == "holder_grudge_cycles")
            {
                //offset = new Vector2(-230, -15);
            }

            var localSpace = ComputeLocalSpace(
                parentComponentRect,
                component.Location.Offset,
                currentState.Width, currentState.Height, 
                component.Location.DockingHorizontal, component.Location.DockingVertical, 
                component.Location.Component_anchor_point, 
                component.Location.Dock_offset);

            var localRect =  new Rectangle((int)localSpace.X, (int)localSpace.Y, (int)currentState.Width, (int)currentState.Height);
            return localRect;
        }

        public static Rectangle GetComponentStateImageLocalCoordinateSpace(TwuiComponentImage image, Rectangle parentComponentRect, Vector2 anchor)
        {


            var localSpace = ComputeLocalSpace2(
                parentComponentRect,
                image.Location.Offset,
                image.Location.Dimensions.X, image.Location.Dimensions.Y,
                image.Location.DockingHorizontal, image.Location.DockingVertical,
                new Vector2(0.0f, 0.0f),
                image.Location.Dock_offset);

            var localRect = new Rectangle((int)localSpace.X, (int)localSpace.Y, (int)image.Location.Dimensions.X, (int)image.Location.Dimensions.Y);
            return localRect;
        }



        static Vector2 ComputeLocalSpace2(Rectangle parentComponentRect, Vector2 offset, float width, float height, DockingHorizontal dockingHorizontal, DockingVertical dockingVertical, Vector2 anchorPoint, Vector2 docking_offset)
        {
            var localSpace = new Vector2(parentComponentRect.X, parentComponentRect.Y);// + offset;

            if (dockingHorizontal != DockingHorizontal.None || dockingVertical != DockingVertical.None)
            {
                switch (dockingHorizontal)
                {
                    case DockingHorizontal.Left:
                        localSpace.X = parentComponentRect.X;
                        break;

                    case DockingHorizontal.Right:
                        localSpace.X = parentComponentRect.Right - width;
                        break;

                    case DockingHorizontal.Center:
                        localSpace.X = parentComponentRect.Left + (parentComponentRect.Width * 0.5f) - (width*.5f);
                        break;
                }

                switch (dockingVertical)
                {
                    case DockingVertical.Top:
                        localSpace.Y = parentComponentRect.Top;
                        break;

                    case DockingVertical.Bottom:
                        localSpace.Y = parentComponentRect.Bottom - height;
                        break;

                    case DockingVertical.Center:
                        localSpace.Y = parentComponentRect.Top + (parentComponentRect.Height * 0.5f) - (height*.5f);
                        break;
                }


                var anchorOffset = new Vector2(width, height) * anchorPoint;
                localSpace -= anchorOffset;
                localSpace += docking_offset;
            }

            return localSpace;

        }






        // We need a new fit for image, to correctly compute the acher points/docking?
        static Vector2 ComputeLocalSpace(Rectangle parentComponentRect, Vector2 offset, float width, float height, DockingHorizontal dockingHorizontal, DockingVertical dockingVertical, Vector2 anchorPoint, Vector2 docking_offset)
        {
            var localSpace = new Vector2(parentComponentRect.X, parentComponentRect.Y);// + offset;

            if (dockingHorizontal != DockingHorizontal.None || dockingVertical != DockingVertical.None)
            {
                switch (dockingHorizontal)
                {
                    case DockingHorizontal.Left:
                        localSpace.X = parentComponentRect.X;
                        break;

                    case DockingHorizontal.Right:
                        localSpace.X = parentComponentRect.Right;
                        break;

                    case DockingHorizontal.Center:
                        localSpace.X = parentComponentRect.Left + (parentComponentRect.Width * 0.5f);
                        break;
                }

                switch (dockingVertical)
                {
                    case DockingVertical.Top:
                        localSpace.Y = parentComponentRect.Top;
                        break;

                    case DockingVertical.Bottom:
                        localSpace.Y = parentComponentRect.Bottom;
                        break;

                    case DockingVertical.Center:
                        localSpace.Y = parentComponentRect.Top + (parentComponentRect.Height * 0.5f);
                        break;
                }


                var anchorOffset = new Vector2(width, height) * anchorPoint;
                localSpace -= anchorOffset;
                localSpace += docking_offset;
            }

            return localSpace;

        }


    }
}


/*
 <states>
				<newstate
					this="75CD9895-19E0-4928-A3586A1349D5DCA5"
					name="NewState"
					width="318"
					height="37"
					interactive="true"
					uniqueguid="75CD9895-19E0-4928-A3586A1349D5DCA5">
					<imagemetrics>
						<image
							this="B7B6DDE2-76D9-48E9-AD75B24A982B3052"
							uniqueguid="B7B6DDE2-76D9-48E9-AD75B24A982B3052"
							componentimage="E87F673A-3B49-44CF-967880690A161A8C"
							offset="-230.00,-15.00"
							width="778"
							height="69"
							tile="true"
							dockpoint="Center"
							dock_offset="0.00,1.00"
							margin="0.00,230.00,0.00,230.00"/>
					</imagemetrics>
				</newstate>
			</states>
 */


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
