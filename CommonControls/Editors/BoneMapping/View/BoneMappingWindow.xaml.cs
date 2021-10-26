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
        public event EventHandler ApplySettings;
        public bool Result { get; private set; } = false;

        public BoneMappingWindow()
        {
            InitializeComponent();
        }

        public BoneMappingWindow(BoneMappingViewModel dataContext, bool showApplyButton)
        {
            DataContext = dataContext;
            InitializeComponent();
            if (showApplyButton == false)
                ApplyButtonHandle.Visibility = Visibility.Collapsed;

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

            Result = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        private void  ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings?.Invoke(this, e);
        }
    }
}

