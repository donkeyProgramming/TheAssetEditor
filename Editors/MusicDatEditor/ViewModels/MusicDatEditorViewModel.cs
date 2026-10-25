using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioExplorer;
using Editors.Audio.Shared.AudioProject;
using Editors.MusicDatEditor.Views;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.MusicDat;

namespace Editors.MusicDatEditor.ViewModels
{
    /// <summary>Editor for campaign_music.dat / battle_music.dat - the compiled music script.
    ///
    /// Two levels, matching the file's own structure: a function list (the call graph) on the
    /// left, and the decompiled instruction stream of the selected function on the right, with
    /// symbol-table reference panels to look up what a raw operand index means. Both views are
    /// read-only - this editor never writes to the file directly, only through the wizards on
    /// the toolbar (Add Culture...), which validate a whole splice before adopting it, so the
    /// file always stays valid.
    ///
    /// Can also be opened with nothing loaded (from the Tools menu), for the sole purpose of
    /// running Add Culture without first hunting down either script in the pack tree - see
    /// <see cref="FindPair"/>.</summary>
    public partial class MusicDatEditorViewModel : ObservableObject, IEditorInterface, IFileEditor, ISaveableEditor
    {
        const string RestrictedToFolder = @"audio\scripts";

        // Where the Audio Editor's own New Audio Project dialog defaults to, so a project made
        // here turns up in the same place a modder would have put one by hand.
        const string AudioProjectFolder = @"audio\audio_projects";

        readonly ILogger _logger = Logging.Create<MusicDatEditorViewModel>();
        readonly IPackFileService _packFileService;
        readonly IFileSaveService _fileSaveService;
        readonly IStandardDialogs _dialogs;
        readonly IEditorManager _editorManager;
        readonly IMusicAudioProjectService _musicAudioProjectService;

        MusicDatFile? _file;
        IReadOnlyDictionary<int, string> _functionNamesByOffset = new Dictionary<int, string>();

        [ObservableProperty] string _displayName = "Music Script Editor";
        [ObservableProperty] bool _hasUnsavedChanges;
        [ObservableProperty] string _statusText = "";
        [ObservableProperty] string _pseudocode = "";

        // True until a file is loaded and says otherwise - this button is also how Add
        // Culture is reached when the editor was opened empty from the Tools menu.
        [ObservableProperty] bool _canAddCulture = true;

        public PackFile CurrentFile { get; private set; } = null!;

        public ObservableCollection<FunctionListItemViewModel> Functions { get; } = [];
        public ObservableCollection<InstructionRowViewModel> Instructions { get; } = [];

        public ObservableCollection<SymbolListItemViewModel> Triggers { get; } = [];
        public ObservableCollection<SymbolListItemViewModel> Variables { get; } = [];
        public ObservableCollection<SymbolListItemViewModel> StringVariables { get; } = [];
        public ObservableCollection<SymbolListItemViewModel> StringConstants { get; } = [];
        public ObservableCollection<SymbolListItemViewModel> Intrinsics { get; } = [];

        FunctionListItemViewModel? _selectedFunction;
        public FunctionListItemViewModel? SelectedFunction
        {
            get => _selectedFunction;
            set
            {
                if (SetProperty(ref _selectedFunction, value))
                {
                    RebuildInstructionRows();
                    RebuildPseudocode();
                }
            }
        }

        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand FindInAudioExplorerCommand { get; }
        public ICommand AddCultureCommand { get; }

        public MusicDatEditorViewModel(IPackFileService packFileService, IFileSaveService fileSaveService,
            IStandardDialogs dialogs, IEditorManager editorManager, IMusicAudioProjectService musicAudioProjectService)
        {
            _packFileService = packFileService;
            _fileSaveService = fileSaveService;
            _dialogs = dialogs;
            _editorManager = editorManager;
            _musicAudioProjectService = musicAudioProjectService;
            OpenCommand = new RelayCommand(Open);
            SaveCommand = new RelayCommand(() => Save());
            FindInAudioExplorerCommand = new RelayCommand<SymbolListItemViewModel>(FindInAudioExplorer);
            AddCultureCommand = new RelayCommand(AddCulture);
        }

        /// <summary>Lets a modder switch (or, from an empty tab, choose) which music script
        /// this editor is looking at, scoped to the one folder either script actually lives
        /// in - the general pack browser has no folder-scoped variant of its own, so this
        /// filters what it returns rather than adding a second copy of that dialog.</summary>
        void Open()
        {
            var result = _dialogs.DisplayBrowseDialog([".dat"]);
            if (!result.Result)
                return;

            var path = _packFileService.GetFullPath(result.File);
            if (!IsInRestrictedFolder(path))
            {
                _dialogs.ShowDialogBox(
                    $"'{result.File.Name}' is not inside {RestrictedToFolder.Replace('\\', '/')}/ - this editor only opens music scripts from there.",
                    "Open");
                return;
            }

            LoadFile(result.File);
        }

        static bool IsInRestrictedFolder(string path) =>
            path.Replace('/', '\\').StartsWith(RestrictedToFolder + "\\", StringComparison.OrdinalIgnoreCase);

        /// <summary>Adds a culture everywhere either script decides one, by copying existing
        /// entries. The wizard hands back an already-validated copy of each file, so this
        /// either adopts a known-good result or leaves the current one untouched.
        ///
        /// A culture has to be wired into both music scripts to work, so both are always
        /// located and offered together (see <see cref="FindPair"/>) regardless of which one,
        /// if either, this tab happens to have open - this is also how the command works when
        /// the editor was opened from the Tools menu with nothing loaded at all. Whichever
        /// file is the one already open in this tab gets its edit left pending like any other
        /// change here; the other has no tab to review it in, so it is committed straight into
        /// the pack the same way the Save button would, and any tab already showing it is
        /// refreshed so it does not go stale.</summary>
        void AddCulture()
        {
            var (battlePackFile, battleFile, campaignPackFile, campaignFile) = FindPair();

            if (battleFile == null && campaignFile == null)
            {
                _dialogs.ShowDialogBox("Could not find battle_music.dat or campaign_music.dat in the loaded packs.", "Add Culture");
                return;
            }

            var slotCount = (battleFile != null ? MusicDatCultureWiring.FindSlots(battleFile).Count : 0) +
                            (campaignFile != null ? MusicDatCultureWiring.FindSlots(campaignFile).Count : 0);
            if (slotCount == 0)
            {
                _dialogs.ShowDialogBox("Neither script contains a culture chain that can be extended.", "Add Culture");
                return;
            }

            var wizard = AddCultureWizardWindow.ShowDialog(Application.Current?.MainWindow, battleFile, campaignFile);
            if (wizard == null)
                return;

            var battle = wizard.EditedBattle;
            var campaign = wizard.EditedCampaign;
            if (battle == null && campaign == null)
                return;

            var parts = new List<string>();
            if (battle != null)
                parts.Add(Commit(battlePackFile!, battle));
            if (campaign != null)
                parts.Add(Commit(campaignPackFile!, campaign));

            if (wizard.CreateAudioProject)
                parts.Add(CreateAudioProject(wizard));

            StatusText = "Added a culture - " + string.Join("; ", parts);
        }

        /// <summary>Generates the audio project holding the events the splice now posts. Run
        /// after the scripts are committed, and reported rather than thrown: the wiring is the
        /// part that had to be all-or-nothing, and it has already succeeded by this point, so
        /// failing to produce the Wwise-side companion is a setback to describe, not a reason
        /// to leave the modder thinking the whole thing came apart.</summary>
        string CreateAudioProject(AddCultureWizardViewModel wizard)
        {
            var events = wizard.EventsNeedingAudio;
            if (events.Count == 0)
                return "no audio project needed (every slot borrows existing audio)";

            try
            {
                var result = _musicAudioProjectService.CreateForMusicEvents(
                    wizard.AudioProjectName, AudioProjectFolder, events);

                var summary = $"wrote {result.FilePath} with {result.CreatedEvents.Count} music event(s)";
                if (result.SkippedEvents.Count == 0)
                    return summary;

                // Nearly always a name that already exists in vanilla, which means the culture
                // is reusing a shipped event rather than getting its own - worth saying out
                // loud, since the script will still post it and it will still play something.
                _dialogs.ShowDialogBox(
                    "The music scripts were wired up, but these events could not be added to the audio project:\n\n  " +
                    string.Join("\n  ", result.SkippedEvents.Select(x => $"{x.EventName} - {x.Reason}")),
                    "Add Culture");
                return summary + $", {result.SkippedEvents.Count} skipped";
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Could not create the audio project: {e.Message}");
                _dialogs.ShowDialogBox(
                    "The music scripts were wired up successfully, but the audio project could not be created:\n\n" +
                    e.Message +
                    "\n\nThe events the scripts now post still need to exist in a sound bank for anything to play.",
                    "Add Culture");
                return "audio project failed";
            }
        }

        /// <summary>Locates both halves of the pair by name across the loaded packs, and
        /// parses whichever are found - preferring the file already open in this tab
        /// (including any pending, not-yet-saved edit) over re-reading it from the pack, so a
        /// second wizard run does not throw away the first one's result.</summary>
        (PackFile? BattlePackFile, MusicDatFile? BattleFile, PackFile? CampaignPackFile, MusicDatFile? CampaignFile) FindPair()
        {
            var battlePackFile = FindMusicDatFile("battle_music.dat");
            var campaignPackFile = FindMusicDatFile("campaign_music.dat");
            return (battlePackFile, ResolveFile(battlePackFile), campaignPackFile, ResolveFile(campaignPackFile));
        }

        /// <summary>The copy the game would load: containers are listed in load order, so the
        /// last match wins, the same precedence <see cref="IPackFileService.FindFile"/> uses.
        /// Taking the first would hand the wizard vanilla's script even when the mod pack
        /// already holds a patched one, and saving would quietly drop the earlier culture.</summary>
        PackFile? FindMusicDatFile(string name) =>
            _packFileService.FindAllWithExtention(".dat")
                .LastOrDefault(x => string.Equals(Path.GetFileName(x.FileName), name, StringComparison.OrdinalIgnoreCase))
                .Pack;

        MusicDatFile? ResolveFile(PackFile? packFile)
        {
            if (packFile == null)
                return null;
            if (_file != null && ReferenceEquals(packFile, CurrentFile))
                return _file;

            try
            {
                return MusicDatParser.Parse(packFile.DataSource.ReadData());
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to load music script {Name}", packFile.Name);
                return null;
            }
        }

        /// <summary>Adopts an edited file as this tab's pending change if it is the one
        /// already open here, or writes it straight into the pack (the same way the Save
        /// button would) otherwise - refreshing any other tab already showing it so that one
        /// does not go stale.</summary>
        string Commit(PackFile packFile, MusicDatFile edited)
        {
            if (_file != null && ReferenceEquals(packFile, CurrentFile))
            {
                _file = edited;
                RebuildAll();
                HasUnsavedChanges = true;
                return $"{packFile.Name} (not saved yet)";
            }

            var bytes = MusicDatParser.Write(edited);
            var path = _packFileService.GetFullPath(packFile);
            var saved = _fileSaveService.Save(path, bytes, prompOnConflict: false);
            if (saved == null)
                return $"{packFile.Name} NOT saved";

            // A script that came from a vanilla pack is saved as a new file in the mod pack,
            // so a tab showing the vanilla one has to move over to it rather than re-read the
            // unpatched original.
            foreach (var editor in _editorManager.GetAllEditors())
                if (editor is MusicDatEditorViewModel vm && ReferenceEquals(vm.CurrentFile, packFile))
                    vm.LoadFile(saved);

            return $"{packFile.Name} (patched directly)";
        }

        /// <summary>Opens a fresh Audio Explorer tab pre-filtered to this event name and selects
        /// the first match, if any - delegates entirely to Audio Explorer's own search/load so this
        /// editor doesn't need its own copy of the (potentially large) Wwise data.</summary>
        void FindInAudioExplorer(SymbolListItemViewModel? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Value))
                return;

            var eventName = item.Value;
            _editorManager.Create(EditorEnums.AudioExplorer_Editor, editor =>
            {
                if (editor is not AudioExplorerViewModel vm)
                    return;

                vm.SearchByActionEvent = true; // resets and reloads the filtered list for Wwise Events
                vm.ExplorerFilter.ExplorerList.Filter = Regex.Escape(eventName);

                var match = vm.ExplorerFilter.ExplorerList.Values.FirstOrDefault();
                if (match != null)
                    vm.ExplorerFilter.ExplorerList.SelectedItem = match;
            });
        }

        public void LoadFile(PackFile file)
        {
            CurrentFile = file;
            DisplayName = file.Name;

            try
            {
                _file = MusicDatParser.Parse(file.DataSource.ReadData());
                RebuildAll();
                HasUnsavedChanges = false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to load music script {Name}", file.Name);
                _dialogs.ShowExceptionWindow(e, $"Failed to load '{file.Name}' as a music script.");
            }
        }

        /// <summary>Re-derives everything shown from <see cref="_file"/>, keeping the selected
        /// function by name. Used both on load and after an edit that moves code around.</summary>
        void RebuildAll()
        {
            if (_file == null)
                return;

            var previouslySelected = SelectedFunction?.Name;
            var functions = MusicDatDisassembler.Disassemble(_file).OrderBy(f => f.Start).ToList();
            _functionNamesByOffset = functions.ToDictionary(f => f.Start, f => f.Name);

            Functions.Clear();
            foreach (var f in functions)
                Functions.Add(new FunctionListItemViewModel(f));

            RebuildSymbolLists();
            CanAddCulture = MusicDatCultureWiring.FindSlots(_file).Count > 0;

            var badCount = functions.Count(f => !f.DecodedExactly);
            StatusText = $"{Functions.Count} functions, {_file.Instructions.Count} instructions" +
                         (badCount > 0 ? $"  ({badCount} FAILED TO DECODE)" : "");

            SelectedFunction = Functions.FirstOrDefault(f => f.Name == previouslySelected) ?? Functions.FirstOrDefault();
        }

        void RebuildSymbolLists()
        {
            Triggers.Clear();
            Variables.Clear();
            StringVariables.Clear();
            StringConstants.Clear();
            Intrinsics.Clear();
            if (_file == null)
                return;

            for (var i = 0; i < _file.Triggers.Count; i++)
                Triggers.Add(new SymbolListItemViewModel(i, _file.Triggers[i].Name, _file.Triggers[i].InitialValue.ToString()));
            for (var i = 0; i < _file.Variables.Count; i++)
                Variables.Add(new SymbolListItemViewModel(i, _file.Variables[i].Name, _file.Variables[i].InitialValue.ToString("0.######")));
            for (var i = 0; i < _file.StringVariables.Count; i++)
                StringVariables.Add(new SymbolListItemViewModel(i, _file.StringVariables[i].Name, _file.StringVariables[i].InitialValue));
            for (var i = 0; i < _file.StringConstants.Count; i++)
                StringConstants.Add(new SymbolListItemViewModel(i, $"[{i}]", _file.StringConstants[i]));
            for (var i = 0; i < _file.Intrinsics.Count; i++)
                Intrinsics.Add(new SymbolListItemViewModel(i, _file.Intrinsics[i].Name, $"{_file.Intrinsics[i].ParameterCount} args"));
        }

        void RebuildInstructionRows()
        {
            Instructions.Clear();
            if (_file == null || SelectedFunction == null)
                return;

            foreach (var ins in SelectedFunction.Function.Instructions)
                Instructions.Add(new InstructionRowViewModel(ins, _file, _functionNamesByOffset));
        }

        void RebuildPseudocode()
        {
            if (_file == null || SelectedFunction == null)
            {
                Pseudocode = "";
                return;
            }
            Pseudocode = MusicDatDecompiler.ToText(_file, SelectedFunction.Function);
        }

        public bool Save()
        {
            if (_file == null)
                return false;

            try
            {
                var bytes = MusicDatParser.Write(_file);
                var path = _packFileService.GetFullPath(CurrentFile);
                var result = _fileSaveService.Save(path, bytes, prompOnConflict: false);
                if (result != null)
                {
                    CurrentFile = result;
                    HasUnsavedChanges = false;
                    StatusText = $"Saved {result.Name}";
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to save music script");
                _dialogs.ShowExceptionWindow(e, "Failed to save the music script. The file on disk is unchanged.");
                return false;
            }
        }

        public void Close()
        {
        }
    }
}
