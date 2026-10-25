using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.MusicProjectEditor.Views;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.MusicDat;

namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>The music project tool: sets up the one music project a mixer mod's maintainer
    /// keeps, shows the cultures in it and the audio each one's events play, and builds it into
    /// the game files - battle_music.dat and campaign_music.dat rebuilt from vanilla with every
    /// culture wired in, plus the music banks, wems and event data.
    ///
    /// The project is held in memory and only written on Save (Build saves first). Opened from
    /// the toolbar it picks up the project at <see cref="MusicAudioProjectService.ProjectFilePath"/>
    /// if the loaded packs have one.</summary>
    public partial class MusicProjectEditorViewModel : ObservableObject, IEditorInterface, IFileEditor, ISaveableEditor
    {
        readonly ILogger _logger = Logging.Create<MusicProjectEditorViewModel>();
        readonly IPackFileService _packFileService;
        readonly IStandardDialogs _dialogs;
        readonly IAudioProjectFileService _audioProjectFileService;
        readonly IMusicAudioProjectService _musicAudioProjectService;
        readonly IMusicScriptBuildService _musicScriptBuildService;
        readonly IAudioProjectCompilerService _audioProjectCompilerService;

        AudioProjectFile? _audioProject;
        string _filePath = MusicAudioProjectService.ProjectFilePath;

        // What each slot is for, read off the game's own scripts the first time it is needed.
        IReadOnlyDictionary<string, string>? _slotTitles;

        [ObservableProperty] string _displayName = "Music Project";
        [ObservableProperty] bool _hasUnsavedChanges;
        [ObservableProperty] bool _isProjectLoaded;
        [ObservableProperty] string _statusText = "";
        [ObservableProperty] string _filePathText = "";

        [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSelectedCulture))]
        MusicCultureViewModel? _selectedCulture;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEditSelectedEventAudio))]
        MusicEventViewModel? _selectedEvent;

        [ObservableProperty] string? _selectedAudioFile;

        public PackFile CurrentFile { get; private set; } = null!;

        public ObservableCollection<MusicCultureViewModel> Cultures { get; } = [];

        public bool HasSelectedCulture => SelectedCulture != null;
        public bool CanEditSelectedEventAudio => SelectedEvent is { IsBorrowed: false, CanCarryOwnAudio: true };

        public string EmptyText =>
            $"No music project is open. New starts one, saved to {MusicAudioProjectService.ProjectFilePath} in the editable pack; " +
            $"Open picks an existing {MusicAudioProjectService.FileExtension} file.";

        public MusicProjectEditorViewModel(
            IPackFileService packFileService,
            IStandardDialogs dialogs,
            IAudioProjectFileService audioProjectFileService,
            IMusicAudioProjectService musicAudioProjectService,
            IMusicScriptBuildService musicScriptBuildService,
            IAudioProjectCompilerService audioProjectCompilerService)
        {
            _packFileService = packFileService;
            _dialogs = dialogs;
            _audioProjectFileService = audioProjectFileService;
            _musicAudioProjectService = musicAudioProjectService;
            _musicScriptBuildService = musicScriptBuildService;
            _audioProjectCompilerService = audioProjectCompilerService;

            var existing = _packFileService.FindFile(MusicAudioProjectService.ProjectFilePath);
            if (existing != null)
                LoadFile(existing);
        }

        public void LoadFile(PackFile file)
        {
            try
            {
                var audioProject = _audioProjectFileService.DeserialiseAudioProject(file);
                CurrentFile = file;
                Show(audioProject, _packFileService.GetFullPath(file));
                StatusText = $"{audioProject.MusicCultures.Count} culture(s)";
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to load music project {Name}", file.Name);
                _dialogs.ShowExceptionWindow(e, $"Failed to load '{file.Name}' as a music project.");
            }
        }

        void Show(AudioProjectFile audioProject, string filePath)
        {
            audioProject.MusicCultures ??= [];
            _audioProject = audioProject;
            _filePath = filePath;

            DisplayName = Path.GetFileName(filePath);
            FilePathText = filePath;
            IsProjectLoaded = true;
            HasUnsavedChanges = false;
            RebuildCultures();
        }

        [RelayCommand]
        void New()
        {
            if (IsProjectLoaded && _dialogs.ShowYesNoBox(
                    $"Start a new, empty music project? Saving it replaces {_filePath}.", "New Music Project") != ShowMessageBoxResult.OK)
                return;

            Show(_musicAudioProjectService.CreateNew(), MusicAudioProjectService.ProjectFilePath);
            HasUnsavedChanges = true;
            StatusText = "New music project - add a culture to start";
        }

        [RelayCommand]
        void Open()
        {
            if (!ConfirmDiscard())
                return;

            var result = _dialogs.DisplayBrowseDialog([MusicAudioProjectService.FileExtension]);
            if (result.Result)
                LoadFile(result.File);
        }

        bool ConfirmDiscard() =>
            !HasUnsavedChanges ||
            _dialogs.ShowYesNoBox("The music project has unsaved changes. Discard them?", "Music Project") == ShowMessageBoxResult.OK;

        [RelayCommand]
        void SaveProject() => Save();

        public bool Save()
        {
            if (_audioProject == null)
                return false;

            if (_packFileService.GetEditablePack() == null)
            {
                _dialogs.ShowDialogBox("No editable pack is selected, so the music project has nowhere to be saved.", "Save");
                return false;
            }

            try
            {
                _audioProjectFileService.Save(_audioProject, Path.GetFileName(_filePath), _filePath);
                CurrentFile = _packFileService.FindFile(_filePath, _packFileService.GetEditablePack()) ?? CurrentFile;
                HasUnsavedChanges = false;
                StatusText = $"Saved {_filePath}";
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to save music project");
                _dialogs.ShowExceptionWindow(e, "Failed to save the music project.");
                return false;
            }
        }

        /// <summary>Adds a culture through the wizard. The wizard is shown the scripts as the next
        /// build would make them before this culture - vanilla with every culture already in the
        /// project - so it can offer those cultures to borrow from and validates against what will
        /// actually be built.</summary>
        [RelayCommand]
        void AddCulture()
        {
            if (_audioProject == null)
                Show(_musicAudioProjectService.CreateNew(), MusicAudioProjectService.ProjectFilePath);

            RunWizard(editing: null);
        }

        [RelayCommand]
        void EditCulture()
        {
            if (SelectedCulture != null)
                RunWizard(SelectedCulture.MusicCulture);
        }

        void RunWizard(MusicCulture? editing)
        {
            var otherCultures = _audioProject!.MusicCultures.Where(musicCulture => musicCulture != editing).ToList();
            var build = _musicScriptBuildService.Build(otherCultures);
            if (build.Problems.Count != 0)
            {
                _dialogs.ShowDialogBox(
                    "The music scripts could not be built from the project, so there is nothing to wire a culture into:\n\n  " +
                    string.Join("\n  ", build.Problems.Take(10)),
                    "Music Project");
                return;
            }

            var wizard = AddCultureWizardWindow.ShowDialog(Application.Current?.MainWindow, build.Battle, build.Campaign, editing);
            if (wizard == null)
                return;

            var musicCulture = wizard.ToMusicCulture();
            MusicCultureAddResult result;
            try
            {
                result = _musicAudioProjectService.AddCulture(_audioProject, musicCulture, replacing: editing);
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Could not add the culture to the music project: {e.Message}");
                _dialogs.ShowDialogBox($"The culture could not be added to the music project:\n\n{e.Message}", "Music Project");
                return;
            }

            // Nearly always a name that already exists in vanilla, which means the culture is
            // reusing a shipped event rather than getting its own - worth saying out loud, since
            // the script will still post it and it will still play something.
            if (result.SkippedEvents.Count != 0)
                _dialogs.ShowDialogBox(
                    "These events could not be added to the music project:\n\n  " +
                    string.Join("\n  ", result.SkippedEvents.Select(skipped => $"{skipped.EventName} - {skipped.Reason}")),
                    "Music Project");

            HasUnsavedChanges = true;
            RebuildCultures(select: musicCulture);
            StatusText = $"{(editing == null ? "Added" : "Updated")} {musicCulture.AudioState} - {result.CreatedEvents.Count} new event(s)";
        }

        [RelayCommand]
        void RemoveCulture()
        {
            if (_audioProject == null || SelectedCulture == null)
                return;

            var musicCulture = SelectedCulture.MusicCulture;
            if (_dialogs.ShowYesNoBox(
                    $"Remove {musicCulture.AudioState} ({musicCulture.SubcultureKey}) from the music project, along with the audio its events were given?",
                    "Remove Culture") != ShowMessageBoxResult.OK)
                return;

            _musicAudioProjectService.RemoveCulture(_audioProject, musicCulture);
            HasUnsavedChanges = true;
            RebuildCultures();
            StatusText = $"Removed {musicCulture.AudioState}";
        }

        /// <summary>Adds a wav from the loaded packs to the selected event. The wav has to be in a
        /// pack already - add it to the editable pack first if it is only on disk.</summary>
        [RelayCommand]
        void AddAudio()
        {
            if (!CanEditSelectedEventAudio)
                return;

            var result = _dialogs.DisplayBrowseDialog([".wav"]);
            if (!result.Result)
                return;

            var wavPackFilePath = _packFileService.GetFullPath(result.File);
            SetSelectedEventAudio([.. SelectedEvent!.AudioFiles, wavPackFilePath]);
        }

        [RelayCommand]
        void RemoveAudio()
        {
            if (!CanEditSelectedEventAudio || SelectedAudioFile == null)
                return;

            var remaining = SelectedEvent!.AudioFiles.ToList();
            remaining.Remove(SelectedAudioFile);
            SetSelectedEventAudio(remaining);
        }

        void SetSelectedEventAudio(IReadOnlyList<string> wavPackFilePaths)
        {
            var musicEvent = SelectedEvent!;
            try
            {
                _musicAudioProjectService.SetAudio(_audioProject!, musicEvent.EventName!, wavPackFilePaths);
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Could not set the audio for {musicEvent.EventName}: {e.Message}");
                _dialogs.ShowDialogBox($"The audio could not be set:\n\n{e.Message}", "Music Project");
                return;
            }

            // All of them rather than just this row: subcultures sharing an Audio State share their
            // battle events, so another culture's row can be showing the same event.
            HasUnsavedChanges = true;
            RebuildCultures();
        }

        /// <summary>Saves the project and compiles it into the editable pack: both music scripts
        /// rebuilt from the game's own copies with every culture wired in, and the music spliced
        /// into the vanilla banks it has to live in, with its wems and event data.</summary>
        [RelayCommand]
        void Build()
        {
            if (_audioProject == null || !Save())
                return;

            try
            {
                using (_dialogs.ShowWaitCursor())
                    _audioProjectCompilerService.Compile(_audioProject.Clean(), Path.GetFileName(_filePath), _filePath);

                StatusText = $"Built {MusicScriptBuildService.BattleScriptPath}, {MusicScriptBuildService.CampaignScriptPath} " +
                             $"and the music banks into {_packFileService.GetEditablePack()!.Name}";
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to build the music project");
                _dialogs.ShowExceptionWindow(e, "The music project could not be built. The project itself was saved.");
                StatusText = "Build failed";
            }
        }

        void RebuildCultures(MusicCulture? select = null)
        {
            var previous = select ?? SelectedCulture?.MusicCulture;
            var previousEventPrefix = SelectedEvent?.Slot.EventPrefix;

            Cultures.Clear();
            if (_audioProject == null)
                return;

            foreach (var musicCulture in _audioProject.MusicCultures)
            {
                var events = musicCulture.Slots
                    .Select(slot => CreateEvent(musicCulture, slot))
                    .ToList();
                Cultures.Add(new MusicCultureViewModel(musicCulture, events));
            }

            SelectedCulture = Cultures.FirstOrDefault(culture => culture.MusicCulture == previous) ?? Cultures.FirstOrDefault();
            SelectedEvent = SelectedCulture?.Events.FirstOrDefault(musicEvent => musicEvent.Slot.EventPrefix == previousEventPrefix)
                ?? SelectedCulture?.Events.FirstOrDefault();
        }

        MusicEventViewModel CreateEvent(MusicCulture musicCulture, MusicCultureSlot slot)
        {
            var audioFiles = slot.ReferenceMatchKey == null
                ? _musicAudioProjectService.GetAudio(_audioProject!, slot.EventPrefix + musicCulture.MusicalCulture)
                : [];

            return new MusicEventViewModel(slot, musicCulture.MusicalCulture, GetSlotTitles().GetValueOrDefault(slot.EventPrefix), audioFiles);
        }

        IReadOnlyDictionary<string, string> GetSlotTitles()
        {
            if (_slotTitles != null)
                return _slotTitles;

            var vanilla = _musicScriptBuildService.Build([]);
            _slotTitles = new[] { vanilla.Battle, vanilla.Campaign }
                .Where(file => file != null)
                .SelectMany(MusicDatCultureWiring.FindSlots)
                .GroupBy(slot => slot.EventPrefix)
                .ToDictionary(group => group.Key, group => group.First().Title);
            return _slotTitles;
        }

        partial void OnSelectedCultureChanged(MusicCultureViewModel? value) => SelectedEvent = value?.Events.FirstOrDefault();

        partial void OnSelectedEventChanged(MusicEventViewModel? value) => SelectedAudioFile = null;

        public void Close()
        {
        }
    }
}
