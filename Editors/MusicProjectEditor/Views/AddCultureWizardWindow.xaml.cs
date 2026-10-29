using System.Windows;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.MusicProjectEditor.ViewModels;
using Shared.GameFormats.MusicDat;

namespace Editors.MusicProjectEditor.Views
{
    public partial class AddCultureWizardWindow : Window
    {
        public AddCultureWizardWindow() => InitializeComponent();

        /// <summary>Runs the wizard over both music scripts and returns the completed view
        /// model, or null if it was cancelled. Neither input is modified - the wizard works on
        /// copies and only returns ones that validated. The whole view model comes back rather
        /// than just the edited pair because the caller also has to act on what the modder
        /// asked for on the Wwise side, which the files themselves cannot express.
        ///
        /// Both files are taken together because a culture has to be wired into both to work:
        /// battle_music.dat decides a battle's theme, campaign_music.dat the map theme and the
        /// ambient layers. Either may be null when only one of the pair could be found, and the
        /// wizard says so rather than silently doing half the job.
        ///
        /// Given a culture to edit, the wizard starts filled in from it.</summary>
        public static AddCultureWizardViewModel? ShowDialog(
            Window? owner, MusicDatFile? battle, MusicDatFile? campaign, MusicCulture? editing = null)
        {
            var viewModel = new AddCultureWizardViewModel(battle, campaign);
            if (editing != null)
                viewModel.LoadFrom(editing);

            var window = new AddCultureWizardWindow
            {
                DataContext = viewModel,
                Owner = owner,
                Title = editing == null ? "Add Culture" : $"Edit Culture - {editing.AudioState}"
            };
            return window.ShowDialog() == true ? viewModel : null;
        }

        void OnAddClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AddCultureWizardViewModel viewModel)
                return;

            // Stays open on failure so the reported problem is still on screen next to the
            // inputs that caused it.
            if (viewModel.TryApply())
            {
                DialogResult = true;
                Close();
            }
        }

        void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
