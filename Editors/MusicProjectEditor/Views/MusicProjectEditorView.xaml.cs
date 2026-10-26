using System.Windows;
using System.Windows.Controls;
using Editors.MusicProjectEditor.ViewModels;

namespace Editors.MusicProjectEditor.Views
{
    public partial class MusicProjectEditorView : UserControl
    {
        public MusicProjectEditorView()
        {
            InitializeComponent();
        }

        // TreeView.SelectedItem cannot be bound, so the selection is passed on from here.
        void OnTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MusicProjectEditorViewModel viewModel && e.NewValue is MusicTreeNodeViewModel node)
                viewModel.SelectedTreeNode = node;
        }
    }
}
