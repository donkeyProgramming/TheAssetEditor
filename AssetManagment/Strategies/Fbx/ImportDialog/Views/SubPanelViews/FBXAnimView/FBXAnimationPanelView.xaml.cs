using System.Windows.Controls;

namespace AssetManagement.Strategies.Fbx.ImportDialog.Views.SubPanelViews.FBXAnimView
{
    /// <summary>
    /// Interaction logic for FBXAnimationSettingsPanel.xaml
    /// </summary>
    public partial class FBXAnimationPanelView : UserControl
    {
        public FBXAnimationPanelView()
        {
            InitializeComponent();
            UpdateLayout();
        }

        // TODO: remove?
        //private void SkeletonComboxBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    SkeletonComboxBox.IsDropDownOpen = true;
        //    //SkeletonComboxBox.Focus();


        //}
        //internal bool HandlingSelectionChange = false;
        //private void SkeletonComboxBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (HandlingSelectionChange)
        //    {
        //        return;
        //    }
            
        //    HandlingSelectionChange = true;

        //    var cb = sender as ComboBox;

        //    if (cb.Text == "click me")
        //    {
        //        cb.Text = "";
        //        e.Handled = true;
        //    }

        //    HandlingSelectionChange = false;
        //}

        //private void SkeletonComboxBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    SkeletonComboxBox.IsDropDownOpen = true;
        //    if (!SkeletonComboxBox.Text.Any())
        //    {
        //        SkeletonComboxBox.IsDropDownOpen = false;
        //    }

        //}

        //private void SkeletonComboxBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        //{

        //    if (e.Key == Key.Down)
        //    {
        //        SkeletonComboxBox.SelectedIndex++;
        //    }
        //}

    }
}

