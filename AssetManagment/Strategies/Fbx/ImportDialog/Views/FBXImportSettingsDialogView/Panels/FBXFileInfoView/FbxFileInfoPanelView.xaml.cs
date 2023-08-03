using System.Windows;
using System.Windows.Controls;


namespace AssetManagement.Strategies.Fbx.ImportDialog.Views.FBXImportSettingsDialogView
{
    /// <summary>
    /// Interaction logic for FileInfoView.xaml
    /// </summary>
    
    public partial class FileInfoView : UserControl
    {
        public FileInfoView()
        {
            InitializeComponent();
            UpdateLayout();

            //Anywhere you plan to bind the list in my case FruitList
            //GetData() creates a collection of Customer data from a database
        }
    }
}
