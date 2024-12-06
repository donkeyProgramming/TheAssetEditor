using System.Windows;
using Shared.Core.Services;

namespace Editors.KitbasherEditor.ChildEditors.VertexDebugger
{
    /// <summary>
    /// Interaction logic for VertexDebuggerWindow.xaml
    /// </summary>
    public partial class VertexDebuggerWindow : Window
    {
        private readonly VertexDebuggerViewModel _viewModel;
        private readonly IWpfGame _game;

        public VertexDebuggerWindow(VertexDebuggerViewModel viewModel, IWpfGame game)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
            _game = game;

            _game.AddComponent(_viewModel);
        }

        public void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _game.RemoveComponent(_viewModel);
        }
    }
}

