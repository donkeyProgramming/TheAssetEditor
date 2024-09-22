using System.Windows;
using System.Windows.Controls;

namespace Shared.Ui.BaseDialogs.Tree
{
    public static class TreeViewBehaviors
    {
        public static readonly DependencyProperty SelectedItemChangedProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItemChanged",
                typeof(object),
                typeof(TreeViewBehaviors),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public static object GetSelectedItemChanged(TreeView treeView)
        {
            return treeView.GetValue(SelectedItemChangedProperty);
        }

        public static void SetSelectedItemChanged(TreeView treeView, object value)
        {
            treeView.SetValue(SelectedItemChangedProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeView = d as TreeView;

            if (treeView != null)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as TreeView;

            if (treeView != null)
            {
                SetSelectedItemChanged(treeView, e.NewValue);
            }
        }
    }
}
