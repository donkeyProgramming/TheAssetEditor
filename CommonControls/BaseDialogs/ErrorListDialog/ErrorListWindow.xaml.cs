using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.BaseDialogs.ErrorListDialog
{
    /// <summary>
    /// Interaction logic for ErrorListWindow.xaml
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        public ErrorListWindow()
        {
            Owner = Application.Current.MainWindow;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowDialog(string titel, ErrorList errorItems, bool modal = true)
        {
            var window = new ErrorListWindow();
            window.DataContext = new ErrorListViewModel()
            {
                WindowTitle = titel + " (" + errorItems.Errors.Count(x => x.IsError) + ")",
                ErrorItems = new ObservableCollection<ErrorListDataItem>(errorItems.Errors)
            };
            if (modal)
                window.ShowDialog();
            else
                window.Show();
        }
    }
}
