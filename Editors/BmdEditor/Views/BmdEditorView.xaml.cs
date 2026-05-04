using System.Windows.Controls;
using Editors.BmdEditor.ViewModels;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace Editors.BmdEditor.Views
{
    /// <summary>
    /// Interaction logic for BmdEditorView.xaml
    /// </summary>
    public partial class BmdEditorView : UserControl
    {
        public BmdEditorView()
        {
            InitializeComponent();
            DataContextChanged += BmdEditorView_DataContextChanged;
        }

        private void BmdEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is INotifyPropertyChanged newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BmdEditorViewModel.SelectedComponent))
            {
                var viewModel = DataContext as BmdEditorViewModel;
                if (viewModel?.SelectedComponent != null)
                {
                    SelectTreeViewItem(viewModel.SelectedComponent);
                }
            }
        }

        private void SelectTreeViewItem(BmdElementViewModel component)
        {
            // Find the TreeViewItem for the component
            var treeViewItem = FindTreeViewItem(ComponentsTreeView, component);
            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                treeViewItem.BringIntoView();
            }
        }

        private TreeViewItem FindTreeViewItem(ItemsControl container, BmdElementViewModel component)
        {
            for (int i = 0; i < container.Items.Count; i++)
            {
                var item = container.Items[i];
                if (item == component)
                {
                    return (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(item);
                }

                // Check if it's a container with children
                if (container.ItemContainerGenerator.ContainerFromItem(item) is ItemsControl childContainer)
                {
                    var result = FindTreeViewItem(childContainer, component);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private void ComponentsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is BmdElementViewModel selectedElement)
            {
                var viewModel = DataContext as BmdEditorViewModel;
                viewModel?.SelectComponent(selectedElement);
            }
        }
    }
}
