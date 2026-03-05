using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Diagnostics;
using Shared.Core.Misc;

namespace Editors.AnimationMeta.Presentation.View
{
    public partial class NewMetaDataEntryWindow : Window
    {
        public NewMetaDataEntryWindow()
        {
            InitializeComponent();
        }

        private void OnOkClick(object sender, RoutedEventArgs e) => HandleOnClick();

        private void HandleOnClick()
        {
            var model = DataContext as NewTagWindowViewModel;
            Guard.IsNotNull(model, $"{nameof(model)} - DataContext must be of type {nameof(NewTagWindowViewModel)}");

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
            Guard.IsNotNull(model, $"{nameof(model)} - DataContext must be of type {nameof(NewTagWindowViewModel)}");
            if (model.SelectedItem != null)
                HandleOnClick();
        }
    }

    class NewTagWindowViewModel : NotifyPropertyChangedImpl
    {
        public ObservableCollection<string> Items { get; set; } = [];
        string? _selectedItem;
        public string? SelectedItem { get => _selectedItem; set { SetAndNotify(ref _selectedItem, value); } }
    }
}
