// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
//using System.Windows.Interactivity;

namespace CommonControls.Behaviors
{

    public static class Ex
    {
        public static TreeViewItem ContainerFromItemRecursive(this ItemContainerGenerator root, object item)
        {
            var treeViewItem = root.ContainerFromItem(item) as TreeViewItem;
            if (treeViewItem != null)
                return treeViewItem;
            foreach (var subItem in root.Items)
            {
                treeViewItem = root.ContainerFromItem(subItem) as TreeViewItem;
                var search = treeViewItem?.ItemContainerGenerator.ContainerFromItemRecursive(item);
                if (search != null)
                    return search;
            }
            return null;
        }

    }

    public class BindableSelectedItemBehavior : Behavior<TreeView>
    {
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(BindableSelectedItemBehavior), new UIPropertyMetadata(null, OnSelectedItemChanged));

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            return;
            var item = e.NewValue as TreeViewItem;

            if (item != null)
            {
                item.SetValue(TreeViewItem.IsSelectedProperty, true);
            }
            else
            {
                var cast = sender as BindableSelectedItemBehavior;

                TreeViewItem treeItem = cast.AssociatedObject
                           .ItemContainerGenerator
                           .ContainerFromItemRecursive(e.NewValue);
                if (treeItem != null)
                    treeItem.SetValue(TreeViewItem.IsSelectedProperty, true);
                else
                {
                    TreeViewItem oldTreeItem = cast.AssociatedObject
                        .ItemContainerGenerator
                        .ContainerFromItemRecursive(e.OldValue);
                    if (oldTreeItem != null)
                        oldTreeItem.SetValue(TreeViewItem.IsSelectedProperty, false);
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.SelectedItem = e.NewValue;
        }
    }
}
