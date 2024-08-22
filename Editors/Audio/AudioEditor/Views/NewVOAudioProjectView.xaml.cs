using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor.Views
{
    /// <summary>
    /// Interaction logic for NewVOAudioProjectView.xaml
    /// </summary>
    public partial class NewVOAudioProjectView : UserControl
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;

        public NewVOAudioProjectView(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;

            InitializeComponent();

            var viewModel = new NewVOAudioProjectViewModel(
                _audioRepository,
                _packFileService,
                _audioEditorViewModel
            );

            // Set the close action for the view model
            viewModel.SetCloseAction(() =>
            {
                var window = Window.GetWindow(this);
                window?.Close();
            });

            DataContext = viewModel;
        }
    }
}
