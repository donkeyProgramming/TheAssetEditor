using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.WaveformVisualiser.Presentation;
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
        readonly IMusicVanillaReferenceService _musicVanillaReferenceService;
        readonly IAudioRepository _audioRepository;

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

        /// <summary>The wavs of whatever the audio list is showing: the selected tree node's for an
        /// event with a tree, the event's own for the rest.</summary>
        public ObservableCollection<string> SelectedAudioFiles { get; } = [];

        // The tree editor, for the selected event when its audio is a decision tree.
        MusicTree? _selectedTree;
        IReadOnlyList<MusicTreeLevel> _treeLevels = [];
        string? _treeLevelsStateGroupName;
        // The node to select once the rows are rebuilt after an edit, for the event it belongs to:
        // the rebuild passes through other events first, which must not use it up.
        (string EventName, int NodeId)? _treeNodeToSelect;
        public ObservableCollection<MusicTreeNodeViewModel> TreeRoots { get; } = [];
        public ObservableCollection<MusicTreeLevel> SplitLevels { get; } = [];
        public ObservableCollection<BranchValueOption> SplitValues { get; } = [];

        [ObservableProperty, NotifyPropertyChangedFor(nameof(CanEditSelectedEventAudio), nameof(CanRemoveBranch), nameof(SplitHelp))]
        MusicTreeNodeViewModel? _selectedTreeNode;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(CanAddBranch))]
        MusicTreeLevel? _selectedSplitLevel;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(CanAddBranch))]
        BranchValueOption? _selectedSplitValue;

        public bool HasSelectedTree => _selectedTree != null;
        public bool CanAddBranch => SelectedTreeNode != null && SelectedSplitLevel != null && SelectedSplitValue != null;
        public bool CanRemoveBranch => SelectedTreeNode is { IsRoot: false };

        public string SplitHelp => SelectedTreeNode?.Node.SplitBy is string splitBy
            ? $"This branch splits by {Wh3MusicTreeLevelInformation.GetLevelName(splitBy)}; add more of its values, or remove the branches below to split by something else."
            : "Give this branch its own music for one value of something, e.g. the battle result being a win. Anything not given its own plays this branch's wavs.";

        // The vanilla reference panel: what the game gives each culture for the selected slot.
        string? _vanillaStateGroupName;
        int _vanillaVersion;
        [ObservableProperty] string? _selectedVanillaState;
        [ObservableProperty] VanillaClipViewModel? _selectedVanillaClip;
        [ObservableProperty] string _vanillaSummary = "";

        public ObservableCollection<string> VanillaStates { get; } = [];
        public ObservableCollection<VanillaClipViewModel> VanillaClips { get; } = [];

        /// <summary>The Audio Explorer's player, for the vanilla clips and the project's own wavs.</summary>
        public WaveformVisualiserViewModel WaveformVisualiserViewModel { get; }

        public PackFile CurrentFile { get; private set; } = null!;

        public ObservableCollection<MusicCultureViewModel> Cultures { get; } = [];

        public bool HasSelectedCulture => SelectedCulture != null;
        public bool CanEditSelectedEventAudio =>
            SelectedEvent is { IsBorrowed: false, CanCarryOwnAudio: true } && (!SelectedEvent.HasTree || SelectedTreeNode != null);

        public string EmptyText =>
            $"No music project is open. New starts one, saved to {MusicAudioProjectService.ProjectFilePath} in the editable pack; " +
            $"Open picks an existing {MusicAudioProjectService.FileExtension} file.";

        public MusicProjectEditorViewModel(
            IPackFileService packFileService,
            IStandardDialogs dialogs,
            IAudioProjectFileService audioProjectFileService,
            IMusicAudioProjectService musicAudioProjectService,
            IMusicScriptBuildService musicScriptBuildService,
            IAudioProjectCompilerService audioProjectCompilerService,
            IMusicVanillaReferenceService musicVanillaReferenceService,
            IAudioRepository audioRepository,
            WaveformVisualiserViewModel waveformVisualiserViewModel)
        {
            _packFileService = packFileService;
            _dialogs = dialogs;
            _audioProjectFileService = audioProjectFileService;
            _musicAudioProjectService = musicAudioProjectService;
            _musicScriptBuildService = musicScriptBuildService;
            _audioProjectCompilerService = audioProjectCompilerService;
            _musicVanillaReferenceService = musicVanillaReferenceService;
            _audioRepository = audioRepository;
            WaveformVisualiserViewModel = waveformVisualiserViewModel;

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

            // Over ten seconds of reading the audio cache, started now so it is done by the time
            // the wizard has been filled in rather than freezing the tool when Add is pressed.
            _ = _musicAudioProjectService.WarmUpAsync();
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
            SetSelectedEventAudio([.. SelectedAudioFiles, wavPackFilePath]);
        }

        [RelayCommand]
        void RemoveAudio()
        {
            if (!CanEditSelectedEventAudio || SelectedAudioFile == null)
                return;

            var remaining = SelectedAudioFiles.ToList();
            remaining.Remove(SelectedAudioFile);
            SetSelectedEventAudio(remaining);
        }

        void SetSelectedEventAudio(IReadOnlyList<string> wavPackFilePaths)
        {
            var musicEvent = SelectedEvent!;
            try
            {
                if (SelectedTreeNode != null)
                {
                    _treeNodeToSelect = (musicEvent.EventName!, SelectedTreeNode.Node.Id);
                    _musicAudioProjectService.SetNodeAudio(_audioProject!, musicEvent.EventName!, SelectedTreeNode.Node.Id, wavPackFilePaths);
                }
                else
                {
                    _musicAudioProjectService.SetAudio(_audioProject!, musicEvent.EventName!, wavPackFilePaths);
                }
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
            var eventName = slot.EventPrefix + musicCulture.MusicalCulture;
            var audioFiles = slot.ReferenceMatchKey == null ? _musicAudioProjectService.GetAudio(_audioProject!, eventName) : [];
            var tree = slot.ReferenceMatchKey == null ? _musicAudioProjectService.GetTree(_audioProject!, eventName) : null;

            return new MusicEventViewModel(slot, musicCulture.MusicalCulture, GetSlotTitles().GetValueOrDefault(slot.EventPrefix), audioFiles, tree);
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

        partial void OnSelectedEventChanged(MusicEventViewModel? value)
        {
            SelectedAudioFile = null;
            LoadTree(value);
            LoadVanillaStates(value?.SlotStateGroupName);
        }

        /// <summary>Shows the event's tree, keeping the node that was selected where it still exists -
        /// every edit rebuilds the rows, and losing your place in the tree after each would be maddening.</summary>
        void LoadTree(MusicEventViewModel? musicEvent)
        {
            var previousTree = _selectedTree;
            var previousNodeId = SelectedTreeNode?.Node.Id;

            TreeRoots.Clear();
            _selectedTree = musicEvent is { HasTree: true, EventName: not null } ? _musicAudioProjectService.GetTree(_audioProject!, musicEvent.EventName) : null;
            OnPropertyChanged(nameof(HasSelectedTree));

            if (_selectedTree == null)
            {
                SelectedTreeNode = null;
                RefreshSelectedAudioFiles();
                return;
            }

            int? nodeToSelect = null;
            if (_treeNodeToSelect is var (pendingEventName, pendingNodeId) && pendingEventName == musicEvent!.EventName)
            {
                nodeToSelect = pendingNodeId;
                _treeNodeToSelect = null;
            }
            else if (previousTree != null && previousTree.StateGroupName == _selectedTree.StateGroupName && previousTree.StateName == _selectedTree.StateName)
            {
                nodeToSelect = previousNodeId;
            }

            // Another slot's levels must not be offered, or added, while this one's load.
            if (_treeLevelsStateGroupName != _selectedTree.StateGroupName)
            {
                _treeLevels = [];
                _treeLevelsStateGroupName = null;
            }

            var root = new MusicTreeNodeViewModel(_selectedTree.Root, null);
            TreeRoots.Add(root);
            var selected = root.SelfAndDescendants().FirstOrDefault(node => node.Node.Id == nodeToSelect) ?? root;
            selected.IsSelected = true;
            SelectedTreeNode = selected;

            LoadTreeLevels(_selectedTree.StateGroupName);
        }

        /// <summary>The levels the tree can split on come from the game's containers, so they wait for
        /// the warm-up the same way the vanilla reference does.</summary>
        async void LoadTreeLevels(string stateGroupName)
        {
            await _musicAudioProjectService.WarmUpAsync();
            if (_selectedTree?.StateGroupName != stateGroupName)
                return;

            try
            {
                using (_dialogs.ShowWaitCursor())
                    _treeLevels = _musicVanillaReferenceService.GetTreeLevels(stateGroupName);
                _treeLevelsStateGroupName = stateGroupName;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to read the levels of {StateGroup}", stateGroupName);
                _treeLevels = [];
            }

            RefreshSplitLevels();
        }

        partial void OnSelectedTreeNodeChanged(MusicTreeNodeViewModel? value)
        {
            RefreshSelectedAudioFiles();
            RefreshSplitLevels();
        }

        void RefreshSelectedAudioFiles()
        {
            SelectedAudioFiles.Clear();
            IEnumerable<string> wavPackFilePaths = SelectedTreeNode != null && SelectedEvent?.EventName != null
                ? _musicAudioProjectService.GetNodeAudio(_audioProject!, SelectedEvent.EventName, SelectedTreeNode.Node.Id).Select(audioFile => audioFile.WavPackFilePath)
                : SelectedEvent?.AudioFiles ?? [];
            foreach (var wavPackFilePath in wavPackFilePaths)
                SelectedAudioFiles.Add(wavPackFilePath);
        }

        /// <summary>What the selected node can split by: the level it already splits by, or any level
        /// not split by above it that exists in a part of the game this node can play in.</summary>
        void RefreshSplitLevels()
        {
            SplitLevels.Clear();
            var node = SelectedTreeNode;
            if (node == null)
            {
                RefreshSplitValues();
                return;
            }

            var splitAbove = node.LevelsSplitAbove().ToHashSet();
            var levels = node.Node.SplitBy != null
                ? _treeLevels.Where(level => level.Name == node.Node.SplitBy)
                : _treeLevels.Where(level => !splitAbove.Contains(level.Name)
                    && (level.ContainerIds.Count == 0 || level.ContainerIds.Any(containerId => !node.IsLimitedToContainerOtherThan(containerId))));

            foreach (var level in levels)
                SplitLevels.Add(level);
            SelectedSplitLevel = SplitLevels.FirstOrDefault();
            RefreshSplitValues();
        }

        partial void OnSelectedSplitLevelChanged(MusicTreeLevel? value) => RefreshSplitValues();

        /// <summary>The values not given a branch yet, then "any other value" if that has none.</summary>
        void RefreshSplitValues()
        {
            SplitValues.Clear();
            var node = SelectedTreeNode?.Node;
            if (node == null || SelectedSplitLevel == null)
            {
                SelectedSplitValue = null;
                return;
            }

            var level = SelectedSplitLevel;
            foreach (var value in level.Values.Where(value => !node.Branches.Any(branch => branch.Value == value)))
                SplitValues.Add(new BranchValueOption(value, Wh3MusicTreeLevelInformation.GetValueName(level.Name, value)));
            if (node.Branches.Count != 0 && !node.Branches.Any(branch => branch.Value == null))
                SplitValues.Add(new BranchValueOption(null, "Any other value"));

            SelectedSplitValue = SplitValues.FirstOrDefault();
        }

        [RelayCommand]
        void AddBranch()
        {
            if (!CanAddBranch || SelectedEvent?.EventName == null)
                return;

            try
            {
                _treeNodeToSelect = (SelectedEvent.EventName, _musicAudioProjectService.AddBranch(_audioProject!, SelectedEvent.EventName, SelectedTreeNode!.Node.Id, SelectedSplitLevel!.Name, SelectedSplitValue!.Value));
            }
            catch (Exception e)
            {
                _dialogs.ShowDialogBox($"The branch could not be added:\n\n{e.Message}", "Music Project");
                return;
            }

            HasUnsavedChanges = true;
            RebuildCultures();
        }

        [RelayCommand]
        void RemoveBranch()
        {
            if (!CanRemoveBranch || SelectedEvent?.EventName == null)
                return;

            var node = SelectedTreeNode!;
            var below = node.SelfAndDescendants().Count() - 1;
            if (_dialogs.ShowYesNoBox(
                    below == 0 ? $"Remove the branch '{node.Label}' and its audio?" : $"Remove the branch '{node.Label}', the {below} below it, and their audio?",
                    "Remove Branch") != ShowMessageBoxResult.OK)
                return;

            try
            {
                _treeNodeToSelect = (SelectedEvent.EventName, node.Parent!.Node.Id);
                _musicAudioProjectService.RemoveBranch(_audioProject!, SelectedEvent.EventName, node.Node.Id);
            }
            catch (Exception e)
            {
                _dialogs.ShowDialogBox($"The branch could not be removed:\n\n{e.Message}", "Music Project");
                return;
            }

            HasUnsavedChanges = true;
            RebuildCultures();
        }

        partial void OnSelectedAudioFileChanged(string? value)
        {
            if (value != null)
                WaveformVisualiserViewModel.SetSelectedPlaylist([value]);
        }

        /// <summary>The vanilla cultures for the slot's State Group, keeping the one picked before
        /// where it is still there - comparing two cultures across slots is the point of the panel.
        ///
        /// Waits for the warm-up first: that reads the repository on another thread, and the
        /// repository cannot be read from two at once. The reading here is then done on this
        /// thread, under a wait cursor - about a second for the first Group, nothing after.</summary>
        async void LoadVanillaStates(string? stateGroupName)
        {
            if (stateGroupName == _vanillaStateGroupName)
                return;

            var version = ++_vanillaVersion;
            _vanillaStateGroupName = stateGroupName;
            var previous = SelectedVanillaState;
            VanillaStates.Clear();
            VanillaClips.Clear();
            VanillaSummary = stateGroupName == null ? "" : "Reading the game's music...";
            if (stateGroupName == null)
                return;

            await _musicAudioProjectService.WarmUpAsync();
            if (version != _vanillaVersion)
                return;

            IReadOnlyList<string> states;
            try
            {
                using (_dialogs.ShowWaitCursor())
                    states = _musicVanillaReferenceService.GetStates(stateGroupName);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to read vanilla music for {StateGroup}", stateGroupName);
                VanillaSummary = $"The game's music could not be read: {e.Message}";
                _vanillaStateGroupName = null; // so selecting the slot again retries
                return;
            }

            foreach (var state in states)
                VanillaStates.Add(state);

            if (states.Count == 0)
                VanillaSummary = "No vanilla culture has music of its own here - are the game's packs loaded?";

            SelectedVanillaState = states.FirstOrDefault(state => string.Equals(state, previous, StringComparison.OrdinalIgnoreCase))
                ?? states.FirstOrDefault();
        }

        partial void OnSelectedVanillaStateChanged(string? value)
        {
            VanillaClips.Clear();
            if (value == null || _vanillaStateGroupName == null)
                return;

            IReadOnlyList<VanillaMusicClip> clips;
            try
            {
                using (_dialogs.ShowWaitCursor())
                    clips = _musicVanillaReferenceService.GetClips(_vanillaStateGroupName, value);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to read vanilla music for {State}", value);
                VanillaSummary = $"The game's music could not be read: {e.Message}";
                return;
            }

            foreach (var clip in clips)
                VanillaClips.Add(new VanillaClipViewModel(clip));

            VanillaSummary = Summarise(clips);
        }

        /// <summary>The numbers a modder wants before supplying their own: how many files, in how
        /// many places, how long they run, and how many are not this culture's alone.</summary>
        static string Summarise(IReadOnlyList<VanillaMusicClip> clips)
        {
            if (clips.Count == 0)
                return "Nothing found for this culture.";

            var files = clips.Select(clip => clip.SourceId).Distinct().Count();
            var places = clips.Select(clip => clip.Where).Distinct().Count();
            var parts = new List<string> { $"{files} file(s) in {places} place(s)" };

            var lengths = clips.Where(clip => clip.DurationMs != null).Select(clip => clip.DurationMs!.Value).Order().ToList();
            if (lengths.Count != 0)
                parts.Add($"{VanillaClipViewModel.FormatSeconds(lengths[0])} to {VanillaClipViewModel.FormatSeconds(lengths[^1])}, " +
                          $"typically {VanillaClipViewModel.FormatSeconds(lengths[lengths.Count / 2])}");

            var shared = clips.Where(clip => clip.AlsoUsedBy.Count != 0).Select(clip => clip.SourceId).Distinct().Count();
            if (shared != 0)
                parts.Add($"{shared} shared with other cultures");

            return string.Join(" · ", parts);
        }

        partial void OnSelectedVanillaClipChanged(VanillaClipViewModel? value)
        {
            if (value == null)
                return;

            var sourceId = value.Clip.SourceId.ToString();
            var source = new WemWaveformSource($"wem:{sourceId}", () => _audioRepository.FindWem(sourceId)?.DataSource.ReadData());
            _ = WaveformVisualiserViewModel.LoadFromWemSourceAsync(source, $"{SelectedVanillaState} - {value.Where}");
        }

        public void Close()
        {
        }
    }
}
