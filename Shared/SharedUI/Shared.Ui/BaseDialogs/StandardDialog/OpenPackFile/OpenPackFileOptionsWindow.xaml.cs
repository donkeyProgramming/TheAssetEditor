using System.Windows;

namespace CommonControls.BaseDialogs
{
    public enum OpenPackFileOption
    {
        OpenPackFile,
        OpenSystemFolder,
        ConvertToSystemFolder
    }

    public partial class OpenPackFileOptionsWindow : Window
    {
        public OpenPackFileOption SelectedOption { get; private set; } = OpenPackFileOption.OpenPackFile;

        public OpenPackFileOptionsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (OpenFolderRadio.IsChecked == true)
                SelectedOption = OpenPackFileOption.OpenSystemFolder;
            else if (ConvertRadio.IsChecked == true)
                SelectedOption = OpenPackFileOption.ConvertToSystemFolder;
            else
                SelectedOption = OpenPackFileOption.OpenPackFile;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
