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

            var rawPath = window.HelpDocumentPath;
            var queryString = "";
            var queryIndex = rawPath.IndexOf('?');
            if (queryIndex >= 0)
            {
                queryString = rawPath.Substring(queryIndex);
                rawPath = rawPath.Substring(0, queryIndex);
            }

            var helpPath = Path.IsPathRooted(rawPath)
                ? rawPath
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath));

            if (!File.Exists(helpPath) && Debugger.IsAttached)
            {
                var searchDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (searchDir?.Parent != null)
                {
                    searchDir = searchDir.Parent;
                    var candidate = Path.Combine(searchDir.FullName, rawPath);
                    if (File.Exists(candidate))
                    {
                        helpPath = candidate;
                        break;
                    }
                }
            }

            if (!File.Exists(helpPath))
                return;

            var fileUri = new Uri(helpPath).AbsoluteUri + queryString;
            Process.Start(new ProcessStartInfo
            {
                FileName = fileUri,
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
