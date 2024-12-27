using System.Windows;
using Shared.Ui.Editors.BoneMapping;
using WindowHandling;

namespace Editors.AnimatioReTarget.Editor.BoneHandling.Presentation
{
    /// <summary>
    /// Interaction logic for BoneMappingWindow.xaml
    /// </summary>
    public partial class BoneMappingWindow : AssetEditorWindow
    {
        public BoneMappingViewModel ViewModel { get; private set; }

        public BoneMappingWindow(BoneMappingViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }


        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            var res = ViewModel.OnOkButton();
            if (res == true)
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
