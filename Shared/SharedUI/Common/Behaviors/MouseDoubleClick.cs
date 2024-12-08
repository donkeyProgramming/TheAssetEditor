// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Shared.Ui.Common.Behaviors
{
    public class MouseDoubleClick
    {
        public static DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command",
            typeof(ICommand),
            typeof(MouseDoubleClick),
            new UIPropertyMetadata(CommandChanged));

        public static DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter",
                                                typeof(object),
                                                typeof(MouseDoubleClick),
                                                new UIPropertyMetadata(null));

        public static void SetCommand(DependencyObject target, ICommand value)
        {
            target.SetValue(CommandProperty, value);
        }

        public static void SetCommandParameter(DependencyObject target, object value)
        {
            target.SetValue(CommandParameterProperty, value);
        }
        public static object GetCommandParameter(DependencyObject target)
        {
            return target.GetValue(CommandParameterProperty);
        }

        private static void CommandChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is TreeViewItem item)
            {
                if (e.NewValue != null)
                {
                    item.MouseDoubleClick += OnMouseDoubleClick;
                }
                else
                {
                    item.MouseDoubleClick -= OnMouseDoubleClick;
                }
            }
        }

        private static void OnMouseDoubleClick(object sender, RoutedEventArgs e)
        {

            if (sender is TreeViewItem item && item.DataContext != null)
            {
                if (item.IsSelected == false)
                    return;

                var command = (ICommand)item.GetValue(CommandProperty);
                var commandParameter = item.GetValue(CommandParameterProperty);
                if (command != null && command.CanExecute(item.DataContext))
                {
                    command.Execute(commandParameter);
                    e.Handled = true;
                }
            }
        }

    }
}
