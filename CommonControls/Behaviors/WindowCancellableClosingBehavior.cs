using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Behaviors
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
        Window window = target as Window;

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
        ICommand closing = GetClosing(sender as Window);
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