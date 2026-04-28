using System;
using System.Linq;
using System.Windows;

namespace GameWorld.Core.WpfWindow.Input
{
    internal static class WindowHelper
    {
        /// <summary>
        /// Returns the window of the given control or null if unable to find a window.
        /// If null, the default implementation is used
        /// </summary>
        /// <returns></returns>
        public static Func<IInputElement, Window> FindWindow = null;

        public static bool IsControlOnActiveWindow(IInputElement element)
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            return activeWindow == null;
        }

        private static Window GetWindowFrom(IInputElement focusElement)
        {
            var finder = FindWindow;
            if (finder != null)
                return finder(focusElement);

            var el = focusElement as FrameworkElement;
            if (el == null)
            {
                // we use D3D11Host which derives from image, so the _focusElement should always be castable to FrameworkElement for us
                throw new NotSupportedException("Only FrameworkElement is currently supported.");
            }
            var controlWindow = Window.GetWindow(el);
            return controlWindow;
        }

    }
}
