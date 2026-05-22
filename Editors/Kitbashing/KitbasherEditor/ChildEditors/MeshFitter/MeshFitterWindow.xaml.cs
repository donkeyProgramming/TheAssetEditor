using System.Windows;
using CommunityToolkit.Diagnostics;
using WindowHandling;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    /// <summary>
    /// Interaction logic for MeshFitterWindow.xaml
    /// </summary>
    public partial class MeshFitterWindow : AssetEditorWindow
    {
        public MeshFitterViewModel? ViewModel { get; set; }

        public MeshFitterWindow(MeshFitterViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        public override void Dispose()
        {
            if (ViewModel != null)
            { 
                ViewModel.Dispose();
                ViewModel = null;
            }
            base.Dispose();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Guard.IsNotNull(ViewModel);

            var res = ViewModel.OnOkButton();
            if (res == true)
                Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
