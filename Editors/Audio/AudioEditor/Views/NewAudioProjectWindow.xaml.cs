using System.Windows;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioEditor.Views
{
    public partial class NewAudioProjectWindow : Window
    {
        private readonly IPackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        private readonly IAudioProjectService _audioProjectService;
        private readonly IStandardDialogs _packFileUiProvider;

        public NewAudioProjectWindow(IPackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
            _audioProjectService = audioProjectService;
            _packFileUiProvider = packFileUiProvider;

            InitializeComponent();
        }

        public static void Show(IPackFileService packFileService, AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IStandardDialogs packFileUiProvider)
        {
            var window = new NewAudioProjectWindow(packFileService, audioEditorViewModel, audioProjectService, packFileUiProvider);
            var newAudioProjectViewModel = new NewAudioProjectViewModel(packFileService, audioEditorViewModel, audioProjectService, packFileUiProvider);

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
