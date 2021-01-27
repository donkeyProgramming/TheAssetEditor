using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TextEditor
{
    public sealed class AvalonEditBehaviour : Behavior<ICSharpCode.AvalonEdit.TextEditor>
    {
        public static readonly DependencyProperty GiveMeTheTextProperty =
            DependencyProperty.Register("GiveMeTheText", typeof(string), typeof(AvalonEditBehaviour),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertyChangedCallback));

        public string GiveMeTheText
        {
            get { return (string)GetValue(GiveMeTheTextProperty); }
            set { SetValue(GiveMeTheTextProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
                AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
                AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
        }

        private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs)
        {
            var textEditor = sender as ICSharpCode.AvalonEdit.TextEditor;
            if (textEditor != null)
            {
                if (textEditor.Document != null)
                    GiveMeTheText = textEditor.Document.Text;
            }
        }

        private static void PropertyChangedCallback(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var behavior = dependencyObject as AvalonEditBehaviour;
            if (behavior.AssociatedObject != null)
            {
               var editor = behavior.AssociatedObject as ICSharpCode.AvalonEdit.TextEditor;
               if (editor.Document != null && dependencyPropertyChangedEventArgs.NewValue != null)
               {
                   var caretOffset = editor.CaretOffset;
                   editor.Document.Text = dependencyPropertyChangedEventArgs.NewValue.ToString();
                   editor.CaretOffset = caretOffset;
               }
            }
        }
    }
}
