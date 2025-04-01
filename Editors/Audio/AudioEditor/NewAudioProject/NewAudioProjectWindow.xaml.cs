using System.Windows;
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

        public static void Show(IPackFileService packFileService, IAudioEditorService audioEditorService, IStandardDialogs packFileUiProvider)
        {
            var window = new NewAudioProjectWindow();
            var newAudioProjectViewModel = new NewAudioProjectViewModel(packFileService, audioEditorService, packFileUiProvider);

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
