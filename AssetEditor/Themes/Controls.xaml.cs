using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WindowHandling;

namespace AssetEditor.Themes
{
    public partial class Controls
    {
        private void CloseWindow_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                this.CloseWind(Window.GetWindow((FrameworkElement)e.Source));
        }

        private void AutoMinimize_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                this.MaximizeRestore(Window.GetWindow((FrameworkElement)e.Source));
        }

        private void Minimize_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                this.MinimizeWind(Window.GetWindow((FrameworkElement)e.Source));
        }

        private void Help_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source == null)
                return;

            var window = Window.GetWindow((FrameworkElement)e.Source) as AssetEditorWindow;
            if (window == null || string.IsNullOrWhiteSpace(window.HelpDocumentPath))
                return;

            var helpPath = Path.IsPathRooted(window.HelpDocumentPath)
                ? window.HelpDocumentPath
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, window.HelpDocumentPath));

            if (File.Exists(helpPath) == false)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = helpPath,
                UseShellExecute = true
            });
        }

        public void CloseWind(Window window) => window?.Close();

        public void MaximizeRestore(Window window)
        {
            if (window == null)
                return;
            switch (window.WindowState)
            {
                case WindowState.Normal:
                    window.WindowState = WindowState.Maximized;
                    break;
                case WindowState.Minimized:
                case WindowState.Maximized:
                    window.WindowState = WindowState.Normal;
                    break;
            }
        }

        public void MinimizeWind(Window window)
        {
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }
    }
}
