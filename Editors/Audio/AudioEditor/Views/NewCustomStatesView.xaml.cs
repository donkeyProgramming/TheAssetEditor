using System.Windows;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor.Views
{
    /// <summary>
    /// Interaction logic for NewCustomStatesView.xaml
    /// </summary>
    public partial class NewCustomStatesView : UserControl
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;

        public NewCustomStatesView(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;

            InitializeComponent();

            var viewModel = new NewCustomStatesViewModel(
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
