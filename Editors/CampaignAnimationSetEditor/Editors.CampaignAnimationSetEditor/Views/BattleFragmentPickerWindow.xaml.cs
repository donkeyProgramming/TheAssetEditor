using System.Windows;
using System.Windows.Input;
using Editors.CampaignAnimationSetEditor.ViewModels;

namespace Editors.CampaignAnimationSetEditor.Views
{
    public partial class BattleFragmentPickerWindow : Window
    {
        public BattleFragmentPickerWindow()
        {
            InitializeComponent();
        }

        void Ok_Click(object sender, RoutedEventArgs e) => Accept();

        void FragmentListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Accept();

        void Accept()
        {
            if (DataContext is BattleFragmentPickerViewModel { SelectedFragment: not null })
                DialogResult = true;
        }
    }
}
