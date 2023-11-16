using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.BaseDialogs
{
    public interface IWindowFactory
    {
        ITypedAssetEditorWindow<TViewModel> Create<TViewModel, TView>(string title, int initialWidth, int initialHeight) where TViewModel : class;
    }

    public class WindowFactory : IWindowFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITypedAssetEditorWindow<TViewModel> Create<TViewModel, TView>(string title, int initialWidth, int initialHeight)
            where TViewModel : class
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            var view = _serviceProvider.GetRequiredService<TView>();

            var containingWindow = new AssetEditorWindow<TViewModel>
            {
                Title = title,
                Width = initialWidth,
                Height = initialHeight,
                DataContext = viewModel,
                Content = view
            };

            return containingWindow;
        }
    }

    public interface IAssetEditorWindow
    {
        public bool AlwaysOnTop { get; set; }
        public void CloseWindow();
        public void ShowWindow();
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
        }
        private void AssetEditorWindow_Deactivated(object sender, EventArgs e)
        {
            if (AlwaysOnTop)
            {
                Window window = (Window)sender;
                window.Topmost = true;
            }
        }







        private void AssetEditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is IDisposable disposable)
                disposable.Dispose();
        }

        public void CloseWindow()
        {
            Close();
        }

        public void ShowWindow()
        {
            Show();
        }
    }
}
