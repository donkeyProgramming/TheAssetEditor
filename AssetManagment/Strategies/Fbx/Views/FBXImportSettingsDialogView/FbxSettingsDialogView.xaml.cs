using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Filetypes.ByteParsing;
using static CommonControls.FormatResearch.DecoderHelper;
using AssetManagement.Strategies.Fbx.Views;

namespace AssetManagement.Strategies.Fbx.Views.FBXImportSettingsDialogView
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
