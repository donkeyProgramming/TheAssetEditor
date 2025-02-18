using System.Windows;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.NewAudioProject
{
    public partial class NewAudioProjectWindow : Window
    {
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IPackFileService _packFileService;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public NewAudioProjectWindow(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _audioEditorViewModel = audioEditorViewModel;
            _packFileService = packFileService;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            InitializeComponent();
        }

        public static void Show(AudioEditorViewModel audioEditorViewModel, IPackFileService packFileService, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            var window = new NewAudioProjectWindow(audioEditorViewModel, packFileService, audioProjectService, packFileUiProvider);
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
