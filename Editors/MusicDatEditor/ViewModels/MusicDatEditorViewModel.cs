using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioExplorer;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.MusicDat;

namespace Editors.MusicDatEditor.ViewModels
{
    /// <summary>Viewer for campaign_music.dat / battle_music.dat - the compiled music script.
    ///
    /// Two levels, matching the file's own structure: a function list (the call graph) on the
    /// left, and the decompiled instruction stream of the selected function on the right, with
    /// symbol-table reference panels to look up what a raw operand index means.
    ///
    /// Read-only throughout. The scripts are never edited by hand: the Music Project tool
    /// rebuilds both from the game's own copies with every culture its project holds, so any
    /// edit made to them directly would be lost on the next build.</summary>
    public partial class MusicDatEditorViewModel : ObservableObject, IEditorInterface, IFileEditor
    {
        const string RestrictedToFolder = @"audio\scripts";

        readonly ILogger _logger = Logging.Create<MusicDatEditorViewModel>();
        readonly IPackFileService _packFileService;
        readonly IStandardDialogs _dialogs;
        readonly IEditorManager _editorManager;

        MusicDatFile? _file;
        IReadOnlyDictionary<int, string> _functionNamesByOffset = new Dictionary<int, string>();

        [ObservableProperty] string _displayName = "Music Script Viewer";
        [ObservableProperty] string _statusText = "";
        [ObservableProperty] string _pseudocode = "";

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
        public ICommand FindInAudioExplorerCommand { get; }

        public MusicDatEditorViewModel(IPackFileService packFileService, IStandardDialogs dialogs, IEditorManager editorManager)
        {
            _packFileService = packFileService;
            _dialogs = dialogs;
            _editorManager = editorManager;
            OpenCommand = new RelayCommand(Open);
            FindInAudioExplorerCommand = new RelayCommand<SymbolListItemViewModel>(FindInAudioExplorer);
        }

        /// <summary>Lets a modder switch (or, from an empty tab, choose) which music script
        /// this viewer is looking at, scoped to the one folder either script actually lives
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
                    $"'{result.File.Name}' is not inside {RestrictedToFolder.Replace('\\', '/')}/ - this viewer only opens music scripts from there.",
                    "Open");
                return;
            }

            LoadFile(result.File);
        }

        static bool IsInRestrictedFolder(string path) =>
            path.Replace('/', '\\').StartsWith(RestrictedToFolder + "\\", StringComparison.OrdinalIgnoreCase);

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
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to load music script {Name}", file.Name);
                _dialogs.ShowExceptionWindow(e, $"Failed to load '{file.Name}' as a music script.");
            }
        }

        /// <summary>Re-derives everything shown from <see cref="_file"/>, keeping the selected
        /// function by name, so switching between the two scripts keeps your place where it can.</summary>
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

        public void Close()
        {
        }
    }
}
