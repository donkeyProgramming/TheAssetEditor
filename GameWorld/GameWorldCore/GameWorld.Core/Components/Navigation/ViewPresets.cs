using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components.Navigation
{

    public enum NavigationAxis
    {
        None = 0,
        PosX = 1,   // +X
        NegX = 2,   // -X
        PosY = 3,   // +Y
        NegY = 4,   // -Y
        PosZ = 5,   // +Z
        NegZ = 6,   // -Z
    }

    /// <summary>
    /// View preset types (corresponding to Blender's numpad views)
    /// </summary>
    public enum ViewPresetType
    {
        Perspective,    // Free perspective view
        Front,          // Numpad 1
        Back,           // Ctrl + Numpad 1
        Right,          // Numpad 3
        Left,           // Ctrl + Numpad 3
        Top,            // Numpad 7
        Bottom,         // Ctrl + Numpad 7
    }

    public static class ViewPresets
    {
        /// <summary>
        /// Get Yaw and Pitch for a specific view preset
        /// </summary>
        public static (float yaw, float pitch) GetViewAngles(ViewPresetType viewType)
        {
            return viewType switch
            {
                ViewPresetType.Perspective => (0.8f, 0.32f),  // Default angled view
                ViewPresetType.Front => (0f, 0f),              // Looking at -Z
                ViewPresetType.Back => (MathHelper.Pi, 0f),    // Looking at +Z
                ViewPresetType.Right => (-MathHelper.PiOver2, 0f),  // Looking at -X
                ViewPresetType.Left => (MathHelper.PiOver2, 0f),    // Looking at +X
                ViewPresetType.Top => (0f, -MathHelper.PiOver2 + 0.01f),   // Looking down (-Y)
                ViewPresetType.Bottom => (0f, MathHelper.PiOver2 - 0.01f), // Looking up (+Y)
                _ => (0.8f, 0.32f)
            };
        }

        /// <summary>
        /// Detect if current camera angles match a view preset
        /// </summary>
        public static ViewPresetType? DetectViewPreset(float yaw, float pitch, float threshold = 0.1f)
        {
            var normalizedYaw = MathHelper.WrapAngle(yaw);
            var absPitch = Math.Abs(pitch);

            // Check top/bottom first (pitch is dominant)
            if (pitch < -MathHelper.PiOver2 + threshold + 0.01f)
                return ViewPresetType.Top;
            if (pitch > MathHelper.PiOver2 - threshold - 0.01f)
                return ViewPresetType.Bottom;

            // Check horizontal views (pitch near 0)
            if (absPitch < threshold)
            {
                if (Math.Abs(normalizedYaw) < threshold)
                    return ViewPresetType.Front;
                if (Math.Abs(normalizedYaw - MathHelper.Pi) < threshold ||
                    Math.Abs(normalizedYaw + MathHelper.Pi) < threshold)
                    return ViewPresetType.Back;
                if (Math.Abs(normalizedYaw + MathHelper.PiOver2) < threshold)
                    return ViewPresetType.Right;
                if (Math.Abs(normalizedYaw - MathHelper.PiOver2) < threshold)
                    return ViewPresetType.Left;
            }

            return null; // Not a preset view
        }

        /// <summary>
        /// Get the view preset for clicking on an axis endpoint
        /// +X -> Right view, -X -> Left view
        /// +Y -> Top view, -Y -> Bottom view
        /// +Z -> Front view, -Z -> Back view
        /// </summary>
        public static ViewPresetType AxisToViewPreset(NavigationAxis axis)
        {
            return axis switch
            {
                NavigationAxis.PosX => ViewPresetType.Right,
                NavigationAxis.NegX => ViewPresetType.Left,
                NavigationAxis.PosY => ViewPresetType.Top,
                NavigationAxis.NegY => ViewPresetType.Bottom,
                NavigationAxis.PosZ => ViewPresetType.Front,
                NavigationAxis.NegZ => ViewPresetType.Back,
                _ => ViewPresetType.Perspective
            };
        }
    }
}
