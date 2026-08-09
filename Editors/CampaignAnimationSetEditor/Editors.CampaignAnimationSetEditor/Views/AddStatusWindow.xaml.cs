using System.Windows;
using Editors.CampaignAnimationSetEditor.ViewModels;

namespace Editors.CampaignAnimationSetEditor.Views
{
    public partial class AddStatusWindow : Window
    {
        public AddStatusWindow()
        {
            InitializeComponent();
        }

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddStatusViewModel vm && !string.IsNullOrWhiteSpace(vm.StatusName))
                DialogResult = true;
        }
    }
}
