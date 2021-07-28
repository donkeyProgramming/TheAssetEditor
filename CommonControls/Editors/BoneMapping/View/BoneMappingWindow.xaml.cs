using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CommonControls.Editors.BoneMapping.View
{
    /// <summary>
    /// Interaction logic for BoneMappingWindow.xaml
    /// </summary>
    public partial class BoneMappingWindow : Window
    {
        public BoneMappingWindow()
        {
            InitializeComponent();
        }

        public BoneMappingWindow(BoneMappingViewModel dataContext)
        {
            DataContext = dataContext;
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as BoneMappingViewModel;
            var res = viewModel.Validate(out string errorText);
            if (res == false)
            {
                var messageBoxResult = MessageBox.Show("Are you sure you want to do this?\n\n" + errorText + "\n\nContinue?", "Error", MessageBoxButton.OKCancel);
                if (messageBoxResult == MessageBoxResult.Cancel)
                    return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

