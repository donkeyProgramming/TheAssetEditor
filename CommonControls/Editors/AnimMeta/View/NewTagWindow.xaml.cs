using Common;
using CommonControls.Common;
using CommonControls.FileTypes.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CommonControls.Editors.AnimMeta.View
{
    /// <summary>
    /// Interaction logic for NewTagWindow.xaml
    /// </summary>
    public partial class NewTagWindow : Window
    {
        public NewTagWindow()
        {
            InitializeComponent();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            var model = DataContext as NewTagWindowViewModel;
            if (model.SelectedItem == null)
            {
                MessageBox.Show("Nothing selected");
                return;
            }

            DialogResult = true;
            Close();

        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var model = DataContext as NewTagWindowViewModel;
            if (model.SelectedItem != null)
                OnOkClick(null, null);
        }
    }

    class NewTagWindowViewModel : NotifyPropertyChangedImpl
    {
        public ObservableCollection<DbTableDefinition> Items { get; set; } = new ObservableCollection<DbTableDefinition>();
        DbTableDefinition _selectedItem;
        public DbTableDefinition SelectedItem { get => _selectedItem; set { SetAndNotify(ref _selectedItem, value); } }
    }
}
