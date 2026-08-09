using System.Windows;
using Editors.CampaignAnimationSetEditor.ViewModels;

namespace Editors.CampaignAnimationSetEditor.Views
{
    public partial class SaveCampaignBinAsWindow : Window
    {
        public SaveCampaignBinAsWindow()
        {
            InitializeComponent();
        }

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SaveCampaignBinAsViewModel vm && !string.IsNullOrWhiteSpace(vm.FileName))
                DialogResult = true;
        }
    }
}
