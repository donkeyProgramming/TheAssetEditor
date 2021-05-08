using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnimationEditor.Common.AnimationPlayer
{
    /// <summary>
    /// Interaction logic for AnimationPlayerView.xaml
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
