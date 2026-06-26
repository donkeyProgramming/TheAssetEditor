using System.Windows;
using System.Windows.Input;

namespace CommonControls.BaseDialogs
{
    public partial class NewPackFileWindow : Window
    {
        public string PackName => NameTextBox.Text;
        public string? SelectedFolderPath { get; private set; }

        public NewPackFileWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to use as a Folder Pack",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedFolderPath = dialog.SelectedPath;
                BrowseFolderButton.Content = System.IO.Path.GetFileName(SelectedFolderPath) ?? SelectedFolderPath;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Key_Down(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
