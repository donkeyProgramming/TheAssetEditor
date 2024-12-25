using System.Windows;
using WindowHandling;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    /// <summary>
    /// Interaction logic for MeshFitterWindow.xaml
    /// </summary>
    public partial class MeshFitterWindow : AssetEditorWindow
    {
        public MeshFitterViewModel ViewModel { get; set; }

        public MeshFitterWindow(MeshFitterViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            var res = ViewModel.OnOkButton();
            if (res == true)
                Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e) => Close();

        private void ApplyButtonClick(object sender, RoutedEventArgs e) => ViewModel.OnApplyButton();
    }
}
