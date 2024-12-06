using System.Windows;
using Editors.Audio.AudioEditor.ViewModels;

namespace Editors.Audio.AudioEditor.Views
{
    /// <summary>
    /// Interaction logic for AudioEditorSettingsWindow.xaml
    /// </summary>
    public partial class AudioEditorSettingsWindow : Window
    {
        private readonly AudioEditorSettingsViewModel _audioEditorSettingsViewModel;

        public AudioEditorSettingsWindow(AudioEditorSettingsViewModel audioEditorSettingsViewModel)
        {
            InitializeComponent();
            _audioEditorSettingsViewModel = audioEditorSettingsViewModel;
            DataContext = _audioEditorSettingsViewModel;
        }
    }
}
