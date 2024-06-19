using System.Windows.Controls;
using Editors.Audio.Presentation.AudioEditor.ViewModels;

namespace Editors.Audio.Presentation.AudioEditor.Views
{
    /// <summary>
    /// Interaction logic for AudioEditorSettingsView.xaml
    /// </summary>
    public partial class AudioEditorSettingsView : UserControl
    {
        public AudioEditorSettingsView()
        {
            InitializeComponent();
            DataContext = new AudioEditorSettingsViewModel();
        }
    }
}
