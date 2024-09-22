using System.Windows;
using Editors.Audio.AudioEditor.ViewModels;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor.Views
{
    public partial class NewAudioProjectWindow : Window
    {
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioProjectService _audioProjectService;

        public NewAudioProjectWindow(PackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
            _audioProjectService = audioProjectService;

            InitializeComponent();
        }

        public static void Show(PackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService)
        {
            var window = new NewAudioProjectWindow(packFileService, audioEditorViewModel, audioProjectService);

            var newAudioProjectViewModel = new NewAudioProjectViewModel(packFileService,audioEditorViewModel,audioProjectService);

            // Set the close action for the ViewModel.
            newAudioProjectViewModel.SetCloseAction(() =>
            {
                window.Close();
            });

            window.DataContext = newAudioProjectViewModel;

            window.ShowDialog();
        }
    }
}
