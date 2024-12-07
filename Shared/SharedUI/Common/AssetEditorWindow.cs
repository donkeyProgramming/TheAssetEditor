using System;
using System.Windows;

// there is a bug in visual studio code generation. Having generated classes with
// Overlapping names (Shared.Ui and Editors.Shared) causes compile errors as the code 
// generator struggls to resolve the naming
namespace WindowHandling 
{
    public  class AssetEditorWindow : Window, IDisposable
    {
        public bool AlwaysOnTop { get; set; } = false;
        bool _isDisposed = false;

        public AssetEditorWindow()
        {
            Owner = Application.Current.MainWindow;
            Deactivated += AssetEdWindow_Deactivated;
        }

        private void AssetEdWindow_Deactivated(object? sender, EventArgs e)
        {
            if (AlwaysOnTop)
            {
                var window = (Window)sender;
                window.Topmost = true;
            }
        }

        public void Dispose()
        {
            if (_isDisposed == false)
                Deactivated -= AssetEdWindow_Deactivated;
            _isDisposed = true;
        }
    }
}
