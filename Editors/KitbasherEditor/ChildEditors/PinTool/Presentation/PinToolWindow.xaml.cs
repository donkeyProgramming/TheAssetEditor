using System.Windows;
using Editors.KitbasherEditor.ChildEditors.PinTool;
using WindowHandling;

namespace Editors.KitbasherEditor.ViewModels.PinTool
{
    public partial class PinToolWindow : AssetEditorWindow
    {
        private readonly PinToolViewModel _viewModel;

        public PinToolWindow(PinToolViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
        }

        private void OnApplyClick(object sender, RoutedEventArgs e)
        {
            var res = _viewModel.Apply();
            if (res == true)
                Close();
        }
    }
}
