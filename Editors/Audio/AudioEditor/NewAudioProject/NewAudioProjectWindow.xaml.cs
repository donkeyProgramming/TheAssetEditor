using System.Windows;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.NewAudioProject
{
    public partial class NewAudioProjectWindow : Window
    {
        public NewAudioProjectWindow()
        {
            InitializeComponent();
        }

        public static void Show(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            var window = new NewAudioProjectWindow();
            var newAudioProjectViewModel = new NewAudioProjectViewModel(audioEditorViewModel, packFileService, audioProjectService, packFileUiProvider);

            // Set the close action for the ViewModel
            newAudioProjectViewModel.SetCloseAction(() =>
            {
                window.Close();
            });

            window.DataContext = newAudioProjectViewModel;

            window.ShowDialog();
        }
    }
}
