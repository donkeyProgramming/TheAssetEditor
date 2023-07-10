using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace KitbasherEditor.Views.EditorViews
{
    /// <summary>
    /// Interaction logic for AnimationView.xaml
    /// </summary>
    public partial class AnimationPlayerView : UserControl
    {
        public AnimationPlayerView()
        {
            InitializeComponent();
            AnimationContent.Visibility = Visibility.Collapsed;
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button.IsChecked == false)
                AnimationContent.Visibility = Visibility.Collapsed;
            else
                AnimationContent.Visibility = Visibility.Visible;
        }
    }
}
