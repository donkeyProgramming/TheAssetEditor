using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KitbasherEditor.ViewModels.MeshFitter;

namespace Editors.KitbasherEditor.ChildEditors.MeshFitter
{
    /// <summary>
    /// Interaction logic for MeshFitterWindow.xaml
    /// </summary>
    public partial class MeshFitterWindow : Window
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
