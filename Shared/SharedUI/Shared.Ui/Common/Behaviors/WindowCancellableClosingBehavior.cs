// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Shared.Ui.Common.Behaviors
{
    public class WindowCancellableClosingBehavior
    {
        public static bool GetIsClosingWithoutPrompt(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsClosingWithoutPromptProperty);
        }

        public static void SetIsClosingWithoutPrompt(DependencyObject obj, bool value)
        {
            obj.SetValue(IsClosingWithoutPromptProperty, value);
        }

        public static readonly DependencyProperty IsClosingWithoutPromptProperty
            = DependencyProperty.RegisterAttached(
            "IsClosingWithoutPrompt", typeof(bool), typeof(WindowCancellableClosingBehavior),
            new UIPropertyMetadata(true));

        public static ICommand GetClosing(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(ClosingProperty);
        }

        public static void SetClosing(DependencyObject obj, ICommand value)
        {
            obj.SetValue(ClosingProperty, value);
        }

        public static readonly DependencyProperty ClosingProperty
            = DependencyProperty.RegisterAttached(
            "Closing", typeof(ICommand), typeof(WindowCancellableClosingBehavior),
            new UIPropertyMetadata(new PropertyChangedCallback(ClosingChanged)));

        private static void ClosingChanged(
          DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var window = target as Window;

            if (window != null)
            {
                if (e.NewValue != null)
                {
                    window.Closing += Window_Closing;
                }
                else
                {
                    window.Closing -= Window_Closing;
                }
            }
        }

        static void Window_Closing(object sender, CancelEventArgs e)
        {
            var closing = GetClosing(sender as Window);
            if (closing != null)
            {
                if (closing.CanExecute(null))
                {
                    closing.Execute(null);
                    e.Cancel = !GetIsClosingWithoutPrompt(sender as Window);
                }
            }
        }
    }
}