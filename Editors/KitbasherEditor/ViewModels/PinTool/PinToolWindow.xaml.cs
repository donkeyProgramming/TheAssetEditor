using System.Windows;

namespace Editors.KitbasherEditor.ViewModels.PinTool
{
    public partial class PinToolWindow : Window
    {
        private readonly PinToolViewModel _viewModel;

        public PinToolWindow(PinToolViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
        }
    }
}
