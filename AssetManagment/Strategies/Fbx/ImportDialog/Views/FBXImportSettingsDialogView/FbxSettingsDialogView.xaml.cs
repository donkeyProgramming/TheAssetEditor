using System.Windows;

namespace AssetManagement.Strategies.Fbx.ImportDialog.Views.FBXImportSettingsDialogView
{
    /// <summary>
    /// Interaction logic for d.xaml
    /// </summary>
    public partial class FBXSetttingsView : Window
    {
        public FBXSetttingsView()
        {
            InitializeComponent();
            



            UpdateLayout();
            ImportButton.Click += ImportButton_Click;

            this.DataContext = this;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)     
        {
            
            DialogResult = true;
            Close();
        }
    }
}
