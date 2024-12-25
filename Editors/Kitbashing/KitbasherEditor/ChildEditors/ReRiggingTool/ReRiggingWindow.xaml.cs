using System.Windows;
using Editors.KitbasherEditor.ChildEditors.ReRiggingTool;
using Shared.Ui.Common;
using WindowHandling;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    /// <summary>
    /// Interaction logic for ReRiggingWindow.xaml
    /// </summary>
    public partial class ReRiggingWindow : AssetEditorWindow
    {
        public ReRiggingViewModel ViewModel { get; set; }

        public ReRiggingWindow(ReRiggingViewModel reRiggingViewModel)
        {
            InitializeComponent();
            ViewModel = reRiggingViewModel;
            DataContext = ViewModel;
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            var res =  ViewModel.OnOkButton();
            if (res == true)
                Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}


