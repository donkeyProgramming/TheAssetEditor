using System.Windows.Controls;
using System.Windows;


namespace AssetManagement.Strategies.Fbx.Views.FBXSettings
{
    /// <summary>
    /// Interaction logic for d.xaml
    /// </summary>
    public partial class FBXSetttingsView : Window
    {
        public FBXSetttingsView()
        {
            InitializeComponent();

            ImportButton.Click += ImportButton_Click;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)     
        {
            
            DialogResult = true;
            Close();
        }

    }
}
