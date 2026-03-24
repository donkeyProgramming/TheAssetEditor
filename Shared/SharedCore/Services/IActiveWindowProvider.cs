// File: Shared/SharedCore/Services/IActiveWindowProvider.cs
using System.Windows;

namespace Shared.Core.Services
{
    /// <summary>
    /// Provides the currently active window in the application.
    /// Used by keyboard handlers to ensure commands are only triggered in the active window.
    /// </summary>
    public interface IActiveWindowProvider
    {
        /// <summary>
        /// Gets the currently active window, or null if no window is active.
        /// </summary>
        Window ActiveWindow { get; }
    }
}
