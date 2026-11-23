using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Editors.CscEditor.ViewModels;

namespace Editors.CscEditor.Views
{
    public partial class CscEditorView : UserControl
    {
        Point _dragStart;
        CscElementViewModel? _dragCandidate;

        CscEditorViewModel? ViewModel => DataContext as CscEditorViewModel;

        public CscEditorView()
        {
            InitializeComponent();
            CurveEditor.CurvesModified += () => ViewModel?.OnCurvesModified();
            DataContextChanged += (_, _) =>
            {
                if (ViewModel != null)
                    ViewModel.RedrawCurves = CurveEditor.Redraw;
            };
        }

        void ComponentTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ViewModel == null)
                return;

            if (e.NewValue is CscElementViewModel vm)
                ViewModel.SelectedElement = vm;
            else if (e.NewValue is CscSceneRootViewModel root)
                ViewModel.SelectedSceneRoot = root;
        }

        void CurveVisibility_Click(object sender, RoutedEventArgs e) => CurveEditor.Redraw();

        void AuxField_LostFocus(object sender, RoutedEventArgs e) => ViewModel?.OnAuxFieldModified();

        // Toolbar buttons don't take focus, so a field being typed in hasn't written its value
        // yet. Commit it first so it becomes its own step instead of being lost or landing on
        // the undone scene.
        void Undo_Click(object sender, RoutedEventArgs e)
        {
            CommitFocusedTextBox();
            ViewModel?.Undo();
        }

        void Redo_Click(object sender, RoutedEventArgs e)
        {
            CommitFocusedTextBox();
            ViewModel?.Redo();
        }

        static void CommitFocusedTextBox()
        {
            if (Keyboard.FocusedElement is TextBox textBox)
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        // ---------------------------------------------------------------------
        // Tree drag-drop re-parenting
        // ---------------------------------------------------------------------

        void ComponentTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(ComponentTree);
            _dragCandidate = FindElementVm(e.OriginalSource);
        }

        void ComponentTree_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragCandidate == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var position = e.GetPosition(ComponentTree);
            if (System.Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                System.Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            var dragged = _dragCandidate;
            _dragCandidate = null;
            DragDrop.DoDragDrop(ComponentTree, new DataObject(typeof(CscElementViewModel), dragged), DragDropEffects.Move);
        }

        void ComponentTree_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(CscElementViewModel)) is not CscElementViewModel dragged)
                return;

            var target = FindElementVm(e.OriginalSource);
            ViewModel?.ReparentElement(dragged, target); // target == null -> detach to top level
            e.Handled = true;
        }

        static CscElementViewModel? FindElementVm(object originalSource)
        {
            var current = originalSource as DependencyObject;
            while (current != null && current is not TreeViewItem)
                current = VisualTreeHelper.GetParent(current);
            return (current as TreeViewItem)?.DataContext as CscElementViewModel;
        }
    }
}
