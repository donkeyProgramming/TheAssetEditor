using System.Windows;
using System.Windows.Input;
using Editors.CampaignAnimationSetEditor.ViewModels;

namespace Editors.CampaignAnimationSetEditor.Views
{
    public partial class OpenCampaignBinWindow : Window
    {
        public OpenCampaignBinWindow()
        {
            InitializeComponent();
        }

        void Ok_Click(object sender, RoutedEventArgs e) => Accept();

        void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Accept();

        void Accept()
        {
            if (DataContext is OpenCampaignBinViewModel { SelectedFile: not null })
                DialogResult = true;
        }
    }
}
