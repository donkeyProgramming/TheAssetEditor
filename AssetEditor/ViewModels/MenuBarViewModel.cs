using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AssetEditor.UiCommands;
using CommonControls.BaseDialogs;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimationFragmentEditor.AnimationPack.Commands;
using Editors.Reports.Animation;
using Editors.Reports.Audio;
using Editors.Reports.Bmd;
using Editors.Reports.DeepSearch;
using Editors.Reports.Files;
using Editors.Reports.Geometry;
using Editors.Shared.Core.Services;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace AssetEditor.ViewModels
{
    public partial class MenuBarViewModel : IDisposable
    {
        private readonly IPackFileService _packfileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IEditorDatabase _editorDatabase;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly TouchedFilesRecorder _touchedFilesRecorder;
        private readonly IFileSaveService _packFileSaveService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IFileSystemAccess _fileSystemAccess;

        public ObservableCollection<RecentPackFileItem> RecentPackFiles { get; set; } = [];
        public ObservableCollection<EditorShortcutViewModel> Editors { get; set; } = [];

        public MenuBarViewModel(IPackFileService packfileService, 
            ApplicationSettingsService settingsService, 
            IEditorDatabase editorDatabase, 
            IUiCommandFactory uiCommandFactory,
            TouchedFilesRecorder touchedFilesRecorder, 
            IFileSaveService packFileSaveService,
            IPackFileContainerLoader packFileContainerLoader,
            IStandardDialogs standardDialogs,
            IFileSystemAccess fileSystemAccess)
        {
            _packfileService = packfileService;
            _settingsService = settingsService;
            _editorDatabase = editorDatabase;
            _uiCommandFactory = uiCommandFactory;
            _touchedFilesRecorder = touchedFilesRecorder;
            _packFileSaveService = packFileSaveService;
            _packFileContainerLoader = packFileContainerLoader;
            _standardDialogs = standardDialogs;
            _fileSystemAccess = fileSystemAccess;
            var settings = settingsService.CurrentSettings;
            settings.RecentPackFilePaths.CollectionChanged += OnRecentPackFilePathsChanged;
            CreateRecentPackFilesItems();
            CreateTools();
        }

        private void OnRecentPackFilePathsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CreateRecentPackFilesItems();
        }

        public void Dispose()
        {
            _settingsService.CurrentSettings.RecentPackFilePaths.CollectionChanged -= OnRecentPackFilePathsChanged;
        }

        [RelayCommand] private void OpenSettingsWindow() => _uiCommandFactory.Create<OpenSettingsDialogCommand>().Execute();
        [RelayCommand] private void OpenPackFile() => _uiCommandFactory.Create<OpenPackFileCommand>().Execute();
        [RelayCommand] private void CreateNewPackFile()
        {
            var window = new NewPackFileWindow();
            if (window.ShowDialog() != true)
                return;

            if (window.SelectedType == NewPackFileType.GamePack)
            {
                if (string.IsNullOrWhiteSpace(window.PackName))
                {
                    _standardDialogs.ShowDialogBox($"'{window.PackName}' is not a valid pack name", "Error");
                    return;
                }

                var currentGame = _settingsService.CurrentSettings.CurrentGame;
                var pfsVersion = GameInformationDatabase.Games[currentGame].PackFileVersion;

                var newPackFile = _packfileService.CreateNewPackFileContainer(window.PackName.Trim(), pfsVersion, PackFileCAType.MOD);
                _packfileService.SetEditablePack(newPackFile);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(window.SelectedFolderPath))
                {
                    _standardDialogs.ShowDialogBox("No folder was selected", "Error");
                    return;
                }

                var folderPack = new SystemFolderContainer(window.SelectedFolderPath, _fileSystemAccess);
                _packfileService.AddContainer(folderPack);
                _packfileService.SetEditablePack(folderPack);
            }
        }
        
        [RelayCommand] private void CreateAnimPackWarhammer3() => _uiCommandFactory.Create<CreateExampleAnimationDbCommand>().CreateAnimationDbWarhammer3();
        [RelayCommand] private void CreateAnimPack3k() => _uiCommandFactory.Create<CreateExampleAnimationDbCommand>().CreateAnimationDb3k();
        [RelayCommand] private void SaveActivePack() => _uiCommandFactory.Create<SavePackFileContainerCommand>().ExecuteForEditablePack();
        [RelayCommand] private void OpenWh2AnimpackUpdater() => new AnimPackUpdaterService(_packfileService).Process();
        [RelayCommand] private void GenerateRmv2Report() => _uiCommandFactory.Create<Rmv2ReportCommand>().Execute();
        [RelayCommand] private void GenerateBmdReport() => _uiCommandFactory.Create<BmdReportCommand>().Execute();
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

        public bool IsDebuggerAttached => Debugger.IsAttached;

        [RelayCommand] private void ClearConsole() => Console.Clear();
        [RelayCommand] private void PrintScope() => _uiCommandFactory.Create<PrintScopesCommand>().Execute();
        [RelayCommand] private void PrintTrackedGraphicsResources() => _uiCommandFactory.Create<PrintTrackedGraphicsResourcesCommand>().Execute();
        [RelayCommand] private void Search() => _uiCommandFactory.Create<DeepSearchCommand>().Execute();
        [RelayCommand] private void OpenAttilaPacks() => _uiCommandFactory.Create<OpenGamePackCommand>(x => x.Configure(GameTypeEnum.Attila)).Execute();
        [RelayCommand] private void OpenRomeRemasteredPacks() => _uiCommandFactory.Create<OpenGamePackCommand>(x => x.Configure(GameTypeEnum.RomeRemastered)).Execute();
        [RelayCommand] private void OpenThreeKingdomsPacks() => _uiCommandFactory.Create<OpenGamePackCommand>(x => x.Configure(GameTypeEnum.ThreeKingdoms)).Execute();
        [RelayCommand] private void OpenWarhammer2Packs() => _uiCommandFactory.Create<OpenGamePackCommand>(x => x.Configure(GameTypeEnum.Warhammer2)).Execute();
        [RelayCommand] private void OpenWarhammer3Packs() => _uiCommandFactory.Create<OpenGamePackCommand>(x => x.Configure(GameTypeEnum.Warhammer3)).Execute();
        [RelayCommand] private void OpenTroyPacks() => _uiCommandFactory.Create<OpenGamePackCommand>(x => x.Configure(GameTypeEnum.Troy)).Execute();

        [RelayCommand] private void OpenAnimatedPropTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.youtube.com/watch?v=b68hSHZ5raY")).Execute();
        [RelayCommand] private void OpenAnimationBasicsTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://youtu.be/H10jDrHJ_Uo?si=XnePs_0X5CQjxLZZ")).Execute();
        [RelayCommand] private void OpenAssetEdBasic0Tutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.youtube.com/watch?v=iVjAVEn8jYc")).Execute();
        [RelayCommand] private void OpenAssetEdBasic1Tutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.youtube.com/watch?v=7HN4oA2LsFM")).Execute();
        [RelayCommand] private void OpenSkragTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.youtube.com/watch?v=MhvbZfNp8Qw")).Execute();
        [RelayCommand] private void OpenTzarGuardTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.youtube.com/watch?v=ONRAKJUmuiM")).Execute();
        [RelayCommand] private void OpenKostalynTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.youtube.com/watch?v=AXw99yc74CY")).Execute();
        [RelayCommand] private void OpenRecolouringModelsTutorial() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://youtu.be/azDq2IRnr1U?si=GammGsisnCzGKYiA")).Execute();

        [RelayCommand]
        private void OpenHelp()
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(appDirectory, "Doc", "index.html");
            Console.WriteLine(fullPath);
            _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure(fullPath)).Execute();
        }
        [RelayCommand] private void OpenModdingWiki() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://tw-modding.com/index.php/Tutorial:AssetEditor")).Execute();
        
        [RelayCommand] private void OpenPatreon() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://www.patreon.com/TheAssetEditor")).Execute();
        [RelayCommand] private void OpenDiscord() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://discord.gg/6Djf2sCczC")).Execute();
        [RelayCommand] private void DownloadRme() => _uiCommandFactory.Create<OpenWebpageCommand>(x => x.Configure("https://github.com/mr-phazer/RME_Release/releases/latest")).Execute();

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
                    var container = _packFileContainerLoader.CreateFromPackFile(PackFileContainerType.Normal, path, false);
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
