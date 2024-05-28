using System;
using System.Security.Policy;
using System.Windows;

namespace Shared.Ui.BaseDialogs.WindowHandling
{
    public interface IAssetEditorWindow
    {
        public bool AlwaysOnTop { get; set; }
        public void CloseWindow();
        public bool? ShowWindow(bool modal = false);
    }

    public interface ITypedAssetEditorWindow<TViewModel>
        : IAssetEditorWindow where TViewModel : class
    {
        public TViewModel TypedContext { get; }
    }

    public class AssetEditorWindow<TViewModel>
        : Window, ITypedAssetEditorWindow<TViewModel> where TViewModel : class
    {
        public bool AlwaysOnTop { get; set; } = false;
        public TViewModel TypedContext
        {
            get => DataContext as TViewModel;
            set => DataContext = value;
        }

        public AssetEditorWindow()
        {
            Style = (Style)FindResource("CustomWindowStyle");
            Closing += AssetEditorWindow_Closing;
            Deactivated += AssetEditorWindow_Deactivated;

            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            // SizeToContent = SizeToContent.WidthAndHeight;
            Owner = Application.Current.MainWindow;
        }

        public void Init()
        {
            if (Content is AssetEditorControl assetEditorControl)
            {
                assetEditorControl.RequestClose += AssetEditorControl_RequestClose;
                assetEditorControl.RequestOK += AssetEditorControl_RequestOK;
            }
        }

        private void AssetEditorControl_RequestOK(object sender, EventArgs e)
        {
            DialogResult = true;
            CloseWindow();
        }

        private void AssetEditorControl_RequestClose(object sender, EventArgs e)
        {
            DialogResult = false;
            CloseWindow();
        }

        private void AssetEditorWindow_Deactivated(object sender, EventArgs e)
        {
            if (AlwaysOnTop)
            {
                var window = (Window)sender;
                window.Topmost = true;
            }
        }

        private void AssetEditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Content is AssetEditorControl assetEditorControl)
            {
                assetEditorControl.RequestClose -= AssetEditorControl_RequestClose;
                assetEditorControl.RequestOK -= AssetEditorControl_RequestOK;
            }

            if (DataContext is IDisposable disposable)
                disposable.Dispose();
        }

        public void CloseWindow()
        {
            Close();
        }

        public bool? ShowWindow(bool modal = false)
        {
            if (modal)
                ShowDialog();
            else
                Show();

            return DialogResult;
        }
    }
}
