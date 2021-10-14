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

namespace CommonControls.SelectionListDialog
{
    /// <summary>
    /// Interaction logic for SelectionListWindow.xaml
    /// </summary>
    public partial class SelectionListWindow : Window
    {
        public bool Result { get; set; } = false;
        public SelectionListWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        public static SelectionListWindow ShowDialog<T>(string titel, IEnumerable<SelectionListViewModel<T>.Item> itemList, bool modal = true)
        {
            var window = new SelectionListWindow();
            var dataContext = new SelectionListViewModel<T>()
            {
                WindowTitle = titel,
            };

            foreach (var item in itemList)
                dataContext.ItemList.Add(item);

            window.DataContext = dataContext;

            if (modal)
                window.ShowDialog();
            else
                window.Show();

            return window;
        }
    }
}
