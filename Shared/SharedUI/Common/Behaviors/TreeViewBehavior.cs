using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shared.Ui.Common.Behaviors
{
    public static class TreeViewBehavior
    {
        public static readonly DependencyProperty ExpandedCommandProperty =
            DependencyProperty.RegisterAttached(
                "ExpandedCommand",
                typeof(ICommand),
                typeof(TreeViewBehavior),
                new PropertyMetadata(null, OnExpandedCommandChanged));

        public static void SetExpandedCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ExpandedCommandProperty, value);
        }

        public static ICommand GetExpandedCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ExpandedCommandProperty);
        }

        private static void OnExpandedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewItem treeViewItem)
            {
                if (e.OldValue != null)
                {
                    treeViewItem.Expanded -= TreeViewItem_Expanded;
                }
                if (e.NewValue != null)
                {
                    treeViewItem.Expanded += TreeViewItem_Expanded;
                }
            }
        }

        private static void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            {
                var command = GetExpandedCommand(treeViewItem);
                if (command != null && command.CanExecute(treeViewItem.DataContext))
                {
                    command.Execute(treeViewItem.DataContext);
                }
            }
        }
    }
}
