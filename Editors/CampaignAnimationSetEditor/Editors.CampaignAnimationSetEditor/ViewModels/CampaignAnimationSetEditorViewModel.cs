using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.CampaignAnimationSetEditor.Services;
using Editors.CampaignAnimationSetEditor.Views;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.AnimationPack;

namespace Editors.CampaignAnimationSetEditor.ViewModels
{
    public partial class CampaignAnimationSetEditorViewModel : ObservableObject, IEditorInterface, IFileEditor, ISaveableEditor
    {
        // Matches the folder this editor is registered against in DependencyInjectionContainer's
        // ValidForFoldersContaining(@"animations\campaign\database").
        const string CampaignBinFolder = @"animations\campaign\database\bin";

        readonly ILogger _logger = Logging.Create<CampaignAnimationSetEditorViewModel>();
        readonly IPackFileService _packFileService;
        readonly IFileSaveService _fileSaveService;
        readonly IStandardDialogs _dialogs;
        readonly CampaignAnimationSetGeneratorService _generatorService;

        [ObservableProperty] string _displayName = "Campaign Animation Set Editor";
        public PackFile CurrentFile { get; private set; } = null!;

        [ObservableProperty] bool _hasUnsavedChanges;
        [ObservableProperty] string? _loadError;
        [ObservableProperty] CampaignAnimationBin? _bin;
        [ObservableProperty] StatusItemViewModel? _selectedStatus;

        // Frozen locomotion/rider clips computed by the last GenerateFromBattleSet() call, not yet
        // written to the pack. Committed alongside the .bin itself in SaveToPath() so that
        // generating a set and then never saving doesn't leave orphaned .anim files behind.
        List<CampaignAnimationSetGeneratorService.PendingAnimationWrite> _pendingAnimationWrites = [];

        /// <summary>Gates the Save/Save As toolbar buttons - there's nothing to write out until a
        /// file has been opened or a set has been generated from a battle animation set.</summary>
        public bool HasOpenFile => Bin != null;

        partial void OnBinChanged(CampaignAnimationBin? value)
        {
            OnPropertyChanged(nameof(HasOpenFile));
            SaveClickedCommand.NotifyCanExecuteChanged();
            SaveAsClickedCommand.NotifyCanExecuteChanged();
        }

        public ObservableCollection<StatusItemViewModel> Statuses { get; } = [];

        public CampaignAnimationSetEditorViewModel(
            IPackFileService packFileService,
            IFileSaveService fileSaveService,
            IStandardDialogs dialogs,
            CampaignAnimationSetGeneratorService generatorService)
        {
            _packFileService = packFileService;
            _fileSaveService = fileSaveService;
            _dialogs = dialogs;
            _generatorService = generatorService;
        }

        public void LoadFile(PackFile file)
        {
            CurrentFile = file;
            DisplayName = file.Name;
            LoadError = null;
            Statuses.Clear();
            Bin = null;
            _pendingAnimationWrites = [];

            try
            {
                var chunk = file.DataSource.ReadDataAsChunk();
                var bin = CampaignAnimationBinLoader.Load(chunk);
                foreach (var status in bin.Status)
                    Statuses.Add(new StatusItemViewModel(status));

                Bin = bin;
                SelectedStatus = Statuses.FirstOrDefault();
                HasUnsavedChanges = false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to parse campaign animation set {Name}", file.Name);
                LoadError = $"This file could not be parsed as a campaign animation set: {e.Message}\n\n" +
                             "It may use an older/unsupported bin version, or contain a data type this editor does not understand.";
            }
        }

        public bool Save()
        {
            if (Bin == null)
                return false;

            // Launched blank from the Tools menu or generated fresh from a battle set, so there's no
            // path yet - fall back to the same pick-a-destination flow Save As uses.
            if (CurrentFile == null)
                return SaveAsInternal();

            return SaveToPath(_packFileService.GetFullPath(CurrentFile));
        }

        /// <summary>The destination folder is fixed (see <see cref="CampaignBinFolder"/>), so this
        /// asks only for a file name. The generic pack-browsing Save dialog can't navigate into a
        /// folder with no files in it yet, which is exactly what a first save into a fresh mod pack
        /// hits.</summary>
        bool SaveAsInternal()
        {
            if (Bin == null)
                return false;

            if (_packFileService.GetEditablePack() == null)
            {
                _dialogs.ShowDialogBox("No editable pack file is selected - pick one in the Pack File Explorer first.", "No editable pack");
                return false;
            }

            var initialName = !string.IsNullOrWhiteSpace(Bin.Reference) ? Bin.Reference : "new_campaign_animation_set";
            var nameViewModel = new SaveCampaignBinAsViewModel(CampaignBinFolder, initialName);
            var nameWindow = new SaveCampaignBinAsWindow { DataContext = nameViewModel };
            if (nameWindow.ShowDialog() != true || string.IsNullOrWhiteSpace(nameViewModel.FileName))
                return false;

            var fileName = Path.GetFileName(nameViewModel.FileName.Trim());
            if (!fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                fileName += ".bin";

            var path = $@"{CampaignBinFolder}\{fileName}";

            if (_packFileService.FindFile(path, _packFileService.GetEditablePack()) != null)
            {
                if (_dialogs.ShowYesNoBox($"'{fileName}' already exists under {CampaignBinFolder}\\. Overwrite it?", "Overwrite file?") != ShowMessageBoxResult.OK)
                    return false;
            }

            return SaveToPath(path);
        }

        bool SaveToPath(string path)
        {
            // Commit frozen clips from the last Generate() first - the entries written below
            // reference them by path. See PendingAnimationWrite for why they're held back.
            foreach (var pending in _pendingAnimationWrites)
                _fileSaveService.Save(pending.Path, pending.Bytes, prompOnConflict: false);
            _pendingAnimationWrites = [];

            foreach (var status in Statuses)
                status.CommitToModel();
            Bin!.Status = Statuses.Select(x => x.Model).ToList();

            var fileNameNoExt = Path.GetFileNameWithoutExtension(path);
            Bin.Reference = fileNameNoExt;

            // Advisory only - shows a warning/error dialog but never blocks the save, matching the
            // behaviour of the XML bin editor this replaces.
            CampaignAnimationSetValidator.ValidateAnimationData(Bin, _packFileService, path);

            var bytes = CampaignAnimationBinLoader.Write(Bin, fileNameNoExt);
            var result = _fileSaveService.Save(path, bytes, prompOnConflict: false);
            if (result == null)
                return false;

            CurrentFile = result;
            DisplayName = result.Name;
            HasUnsavedChanges = false;
            return true;
        }

        [RelayCommand(CanExecute = nameof(HasOpenFile))]
        void SaveClicked() => Save();

        [RelayCommand(CanExecute = nameof(HasOpenFile))]
        void SaveAsClicked() => SaveAsInternal();

        [RelayCommand]
        void Open()
        {
            if (HasUnsavedChanges)
            {
                if (_dialogs.ShowYesNoBox("You have unsaved changes that will be lost. Open a different file anyway?", "Open") != ShowMessageBoxResult.OK)
                    return;
            }

            // The app's generic pack-browse dialog has no folder scoping and would list every .bin
            // in the game, so this uses its own picker pre-filtered to CampaignBinFolder.
            var pickerViewModel = new OpenCampaignBinViewModel(_packFileService, CampaignBinFolder);
            var window = new OpenCampaignBinWindow { DataContext = pickerViewModel };
            if (window.ShowDialog() != true || pickerViewModel.SelectedFile == null)
                return;

            LoadFile(pickerViewModel.SelectedFile.File);
        }

        [RelayCommand]
        void AddStatus()
        {
            var pickerViewModel = new AddStatusViewModel(Statuses.Select(x => x.Name));
            var window = new AddStatusWindow { DataContext = pickerViewModel };
            if (window.ShowDialog() != true)
                return;

            var name = pickerViewModel.StatusName.Trim();
            if (Statuses.Any(x => x.Name == name))
            {
                _dialogs.ShowDialogBox($"A status named '{name}' already exists.", "Add status");
                return;
            }

            var vm = StatusItemViewModel.CreateNew(name);
            Statuses.Add(vm);
            SelectedStatus = vm;
            HasUnsavedChanges = true;
        }

        [RelayCommand]
        void RemoveStatus(StatusItemViewModel? status)
        {
            status ??= SelectedStatus;
            if (status == null)
                return;

            if (_dialogs.ShowYesNoBox($"Remove status '{status.Name}'?", "Remove status") != ShowMessageBoxResult.OK)
                return;

            Statuses.Remove(status);
            if (SelectedStatus == status)
                SelectedStatus = Statuses.FirstOrDefault();
            HasUnsavedChanges = true;
        }

        [RelayCommand]
        void GenerateFromBattleSet()
        {
            var pickerViewModel = new BattleFragmentPickerViewModel(_packFileService);
            var window = new BattleFragmentPickerWindow { DataContext = pickerViewModel };
            if (window.ShowDialog() != true || pickerViewModel.SelectedFragment == null)
                return;

            if (Statuses.Count > 0)
            {
                if (_dialogs.ShowYesNoBox("This replaces every status currently in the editor with a freshly generated set. Continue?", "Generate from battle animation set") != ShowMessageBoxResult.OK)
                    return;
            }

            // Matches vanilla's naming convention (cam_hu1_sword_and_shield for the
            // hu1_sword_and_shield battle set), and becomes the suggested Save As name since
            // SaveAsInternal seeds itself from Bin.Reference.
            var reference = "cam_" + Path.GetFileNameWithoutExtension(pickerViewModel.SelectedFragment.FullPath);

            CampaignAnimationSetGeneratorService.GenerateResult generateResult;
            try
            {
                generateResult = _generatorService.Generate(pickerViewModel.SelectedFragment, reference, pickerViewModel.FreezeRootForRiderAnimations, pickerViewModel.ForceGroundAnimations);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to generate campaign animation set from battle set {Fragment}", pickerViewModel.SelectedFragment.FullPath);
                _dialogs.ShowDialogBox($"Failed to generate a campaign animation set from this battle animation set:\n{e.Message}", "Generate failed");
                return;
            }

            Statuses.Clear();
            foreach (var status in generateResult.Bin.Status)
                Statuses.Add(new StatusItemViewModel(status));

            Bin = generateResult.Bin;
            // Discards anything pending from a previous unsaved Generate - none of it was written.
            _pendingAnimationWrites = generateResult.PendingAnimationWrites.ToList();
            SelectedStatus = Statuses.FirstOrDefault();
            LoadError = null;
            HasUnsavedChanges = true;
        }

        public void Close()
        {
        }
    }
}
