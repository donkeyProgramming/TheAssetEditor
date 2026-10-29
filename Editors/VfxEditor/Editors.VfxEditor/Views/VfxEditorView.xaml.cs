using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Editors.VfxEditor.ViewModels;

namespace Editors.VfxEditor.Views
{
    public partial class VfxEditorView : UserControl
    {
        public VfxEditorView()
        {
            InitializeComponent();
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is VfxEditorViewModel old)
                    old.FlushPendingEdits = null;
                if (e.NewValue is VfxEditorViewModel now)
                    now.FlushPendingEdits = FlushPendingEdits;
            };
        }

        void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is VfxEditorViewModel viewModel)
                viewModel.SelectedNode = e.NewValue as VfxTreeNode;
        }

        // A key row is picked when one of its boxes takes focus, and stays picked when the focus moves on to a button.
        void KeyRow_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ((ListBoxItem)sender).IsSelected = true;

        // Collected first: writing one value can rebuild the rows.
        void FlushPendingEdits()
        {
            var pending = Descendants(this).OfType<TextBox>()
                .Select(box => box.GetBindingExpression(TextBox.TextProperty))
                .Where(binding => binding is { IsDirty: true })
                .ToList();
            foreach (var binding in pending)
                binding!.UpdateSource();
        }

        static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in Descendants(child))
                    yield return descendant;
            }
        }
    }
}
