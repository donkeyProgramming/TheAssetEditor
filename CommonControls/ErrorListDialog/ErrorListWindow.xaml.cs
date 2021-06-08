using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using static CommonControls.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.ErrorListDialog
{
    /// <summary>
    /// Interaction logic for ErrorListWindow.xaml
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        public ErrorListWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowDialog(string titel, ErrorList errorItems)
        {
            var window = new ErrorListWindow();
            window.DataContext = new ErrorListViewModel()
            {
                WindowTitle = titel + " (" + errorItems.Errors.Count(x=>x.IsError) + ")",
                ErrorItems = errorItems.Errors
            };
            window.ShowDialog();
        }
    }
}
