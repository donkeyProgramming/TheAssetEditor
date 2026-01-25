using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectEditor;
using Editors.Audio.AudioEditor.Presentation.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Presentation.AudioProjectViewer;
using Editors.Audio.AudioEditor.Presentation.Settings;
using Editors.Audio.AudioEditor.Presentation.WaveformVisualiser;

namespace Editors.Audio.AudioEditor.Presentation
{
    public enum FocussedViewModel
    {
        Unknown,
        AudioProjectExplorer,
        AudioFilesExplorer,
        AudioProjectEditor,
        AudioProjectViewer,
        Settings,
        WaveformVisualiser
    }

    public partial class AudioEditorView : UserControl
    {
        public AudioEditorViewModel ViewModel => DataContext as AudioEditorViewModel;

        private Window _window;

        public AudioEditorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            _window = Window.GetWindow(this);
            if (_window == null)
                return;

            _window.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnWindowPreviewKeyDown), true);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_window == null)
                return;

            _window.RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnWindowPreviewKeyDown));
            _window = null;
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focussedViewModel = GetFocussedViewModel();

            var isTextInputFocussed = Keyboard.FocusedElement is ComboBox comboBox && comboBox.IsEditable || Keyboard.FocusedElement is TextBox textBox && !textBox.IsReadOnly;

            var isSettingsAudioFilesListViewFocussed = false;
            if (focussedViewModel == FocussedViewModel.Settings)
                isSettingsAudioFilesListViewFocussed = IsAudioFilesListViewFocussed(e.OriginalSource as DependencyObject);

            ViewModel?.OnPreviewKeyDown(e, isTextInputFocussed, isSettingsAudioFilesListViewFocussed);
        }

        private static bool IsAudioFilesListViewFocussed(DependencyObject source)
        {
            var current = source;
            while (current != null)
            {
                if (current is FrameworkElement frameworkElement && frameworkElement.Name == "AudioFilesListView")
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private static FocussedViewModel GetFocussedViewModel()
        {
            var currentElement = Keyboard.FocusedElement as DependencyObject;
            while (currentElement != null)
            {
                if (currentElement is UserControl userControl)
                {
                    var dataContext = userControl.DataContext;
                    if (dataContext is AudioProjectExplorerViewModel)
                        return FocussedViewModel.AudioProjectExplorer;
                    else if (dataContext is AudioFilesExplorerViewModel)
                        return FocussedViewModel.AudioFilesExplorer;
                    else if (dataContext is AudioProjectEditorViewModel)
                        return FocussedViewModel.AudioProjectEditor;
                    else if (dataContext is AudioProjectViewerViewModel)
                        return FocussedViewModel.AudioProjectViewer;
                    else if (dataContext is SettingsViewModel)
                        return FocussedViewModel.Settings;
                    else if (dataContext is WaveformVisualiserViewModel)
                        return FocussedViewModel.WaveformVisualiser;
                    else 
                        return FocussedViewModel.Unknown;
                }
                currentElement = VisualTreeHelper.GetParent(currentElement);
            }
            return FocussedViewModel.Unknown;
        }
    }
}
