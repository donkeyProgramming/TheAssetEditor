// File: Shared/SharedCore/Services/ActiveWindowProvider.cs
using System.Windows;

namespace Shared.Core.Services
{
    /// <summary>
    /// Default implementation of IActiveWindowProvider.
    /// Returns the currently active window from Application.Current.Windows.
    /// </summary>
    public class ActiveWindowProvider : IActiveWindowProvider
    {
        public Window ActiveWindow
        {
            get
            {
                if (Application.Current == null)
                    return null;

                foreach (Window window in Application.Current.Windows)
                {
                    if (window.IsActive)
                        return window;
                }

                return null;
            }
        }
    }
}
