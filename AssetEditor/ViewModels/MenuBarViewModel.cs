using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AssetEditor.UiCommands;
using CommonControls.BaseDialogs;
using CommonControls.Editors.AnimationPack;
using CommunityToolkit.Mvvm.Input;
using Editors.Reports.Animation;
using Editors.Reports.Audio;
using Editors.Reports.DeepSearch;
using Editors.Reports.Files;
using Editors.Reports.Geometry;
using Editors.Shared.Core.Services;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace AssetEditor.ViewModels
{
    public partial class MenuBarViewModel
    {
        private readonly IPackFileService _packfileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IEditorDatabase _editorDatabase;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly TouchedFilesRecorder _touchedFilesRecorder;
        private readonly IFileSaveService _packFileSaveService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IStandardDialogs _standardDialogs;

        public ObservableCollection<RecentPackFileItem> RecentPackFiles { get; set; } = [];
        public ObservableCollection<EditorShortcutViewModel> Editors { get; set; } = [];

        public MenuBarViewModel(IPackFileService packfileService, 
            ApplicationSettingsService settingsService, 
            IEditorDatabase editorDatabase, 
            IUiCommandFactory uiCommandFactory,
            TouchedFilesRecorder touchedFilesRecorder, 
            IFileSaveService packFileSaveService,
            IPackFileContainerLoader packFileContainerLoader,
            IStandardDialogs standardDialogs)
        {
            _packfileService = packfileService;
            _settingsService = settingsService;
            _editorDatabase = editorDatabase;
            _uiCommandFactory = uiCommandFactory;
            _touchedFilesRecorder = touchedFilesRecorder;
            _packFileSaveService = packFileSaveService;
            _packFileContainerLoader = packFileContainerLoader;
            _standardDialogs = standardDialogs;
            var settings = settingsService.CurrentSettings;
            settings.RecentPackFilePaths.CollectionChanged += (sender, args) => CreateRecentPackFilesItems();
            CreateRecentPackFilesItems();
            CreateTools();
        }

        [RelayCommand] private void OpenSettingsWindow() => _uiCommandFactory.Create<OpenSettingsDialogCommand>().Execute();
        [RelayCommand] private void OpenPackFile() => _uiCommandFactory.Create<OpenPackFileCommand>().Execute();
        [RelayCommand] private void CreateNewPackFile()
        {
            var window = new TextInputWindow("New Pack Name", "");
            if (window.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(window.TextValue))
                {
                    _standardDialogs.ShowDialogBox($"'{window.TextValue}' is not a valid packfile name", "Error");
                    return;
                }

                var newPackFile = _packfileService.CreateNewPackFileContainer(window.TextValue.Trim(), PackFileCAType.MOD);
                _packfileService.SetEditablePack(newPackFile);
            }
        }

        [RelayCommand] private void CreateAnimPackWarhammer3() => AnimationPackSampleDataCreator.CreateAnimationDbWarhammer3(_packFileSaveService, _packfileService);
        [RelayCommand] private void CreateAnimPack3k() => AnimationPackSampleDataCreator.CreateAnimationDb3k(_packfileService, _packFileSaveService);
        [RelayCommand] private void SaveActivePack() => _uiCommandFactory.Create<SavePackFileContainerCommand>().Execute();
        [RelayCommand] private void OpenWh2AnimpackUpdater() => new AnimPackUpdaterService(_packfileService).Process();
        [RelayCommand] private void GenerateRmv2Report() => _uiCommandFactory.Create<Rmv2ReportCommand>().Execute();
        [RelayCommand] private void GenerateMetaDataReport() => _uiCommandFactory.Create<GenerateMetaDataReportCommand>().Execute();
        [RelayCommand] private void GenerateFileListReport() => _uiCommandFactory.Create<FileListReportCommand>().Execute();
        [RelayCommand] private void GenerateMetaDataJsonsReport() => _uiCommandFactory.Create<GenerateMetaJsonDataReportCommand>().Execute();
        [RelayCommand] private void GenerateMaterialReport() => _uiCommandFactory.Create<MaterialReportCommand>().Execute();
        [RelayCommand] private void GenerateDialogueEventInfoPrinterReport() => _uiCommandFactory.Create<GenerateDialogueEventInfoPrinterReportCommand>().Execute();
        [RelayCommand] private void GenerateDialogueEventAndEventNamePrinterReport() => _uiCommandFactory.Create<GenerateDialogueEventAndEventNamePrinterReportCommand>().Execute();
        [RelayCommand] private void GenerateDatDumperReport() => _uiCommandFactory.Create<GenerateDatDumperReportCommand>().Execute();


        [RelayCommand] private void TouchedFileRecorderStart() => _touchedFilesRecorder.Start();
        [RelayCommand] private void TouchedFileRecorderPrint() => _touchedFilesRecorder.Print();
        [RelayCommand] private void TouchedFileRecorderExtract() => _touchedFilesRecorder.ExtractFilesToPack(@"c:\temp\extractedPack.pack");
        [RelayCommand] private void TouchedFileRecorderStop() => _touchedFilesRecorder.Stop();

        [RelayCommand] private void ClearConsole() => Console.Clear();
        [RelayCommand] private void PrintScope() => _uiCommandFactory.Create<PrintScopesCommand>().Execute();
        [RelayCommand] private void Search() => _uiCommandFactory.Create<DeepSearchCommand>().Execute();
        [RelayCommand] private void OpenAttilaPacks() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Attila);
        [RelayCommand] private void OpenRomeRemasteredPacks() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.RomeRemastered);
        [RelayCommand] private void OpenThreeKingdomsPacks() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.ThreeKingdoms);
        [RelayCommand] private void OpenWarhammer2Packs() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Warhammer2);
        [RelayCommand] private void OpenWarhammer3Packs() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Warhammer3);
        [RelayCommand] private void OpenTroyPacks() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Troy);

        [RelayCommand] private void OpenAnimatedPropTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=b68hSHZ5raY");
        [RelayCommand] private void OpenAnimationBasicsTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://youtu.be/H10jDrHJ_Uo?si=XnePs_0X5CQjxLZZ");
        [RelayCommand] private void OpenAssetEdBasic0Tutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=iVjAVEn8jYc");
        [RelayCommand] private void OpenAssetEdBasic1Tutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=7HN4oA2LsFM");
        [RelayCommand] private void OpenSkragTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=MhvbZfNp8Qw");
        [RelayCommand] private void OpenTzarGuardTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=ONRAKJUmuiM");
        [RelayCommand] private void OpenKostalynTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=AXw99yc74CY");
        [RelayCommand] private void OpenRecolouringModelsTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://youtu.be/azDq2IRnr1U?si=GammGsisnCzGKYiA");

        [RelayCommand] private void OpenHelp() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://tw-modding.com/index.php/Tutorial:AssetEditor");
        [RelayCommand] private void OpenPatreon() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.patreon.com/TheAssetEditor");
        [RelayCommand] private void OpenDiscord() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://discord.gg/6Djf2sCczC");
        [RelayCommand] private void DownloadRme() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://github.com/mr-phazer/RME_Release/releases/latest");

        [RelayCommand] private static void OpenAssetEditorFolder() => Process.Start("explorer.exe", DirectoryHelper.ApplicationDirectory);

        [RelayCommand] private static void ClearAssetEditorFolder()
        {
            try { Directory.Delete(DirectoryHelper.ApplicationDirectory, true); } catch { }
        }

        [RelayCommand] private void TogglePackFileExplorer() => _uiCommandFactory.Create<TogglePackFileExplorerCommand>().Execute();

        void CreateRecentPackFilesItems()
        {
            var settings = _settingsService.CurrentSettings;

            RecentPackFiles.Clear();
            var menuItemViewModels = settings.RecentPackFilePaths.Select(path => new RecentPackFileItem(
                path,
                () =>
                {
                    var container = _packFileContainerLoader.Load(path);
                    if (container == null)
                    {
                        System.Windows.MessageBox.Show($"Unable to load packfiles {path}");
                        return;
                    }

                    _packfileService.AddContainer(container, true);
                        
                }
            ));
            foreach (var menuItem in menuItemViewModels.Reverse())
            {
                RecentPackFiles.Add(menuItem);
            }
        }

        void CreateTools()
        {
            var infos = _editorDatabase
                .GetEditorInfos()
                .OrderBy(x=>x.ToolbarName)
                .Where(x=>x.AddToolbarButton)
                .ToList();

            foreach (var item in infos)
                Editors.Add(new EditorShortcutViewModel(item, _uiCommandFactory));   
        }
    }
}
