using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SkeletonEditor;
using AnimationEditor.SuperView;
using AssetEditor.Report;
using AssetEditor.UiCommands;
using AssetEditor.Views.Settings;
using Audio.Presentation.AudioExplorer;
using Audio.Presentation.Compiler;
using CommonControls.BaseDialogs;
using CommonControls.Common;
using CommonControls.Editors.AnimationBatchExporter;
using CommonControls.Editors.AnimationPack;
using CommonControls.Events.UiCommands;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    public class RecentPackFileItem
    {
        public RecentPackFileItem(string path, Action execute)
        {
            Command = new RelayCommand(execute);
            Header = System.IO.Path.GetFileName(path);
        }

        public string Header { get; set; }
        public string Path { get; set; }

        public ICommand Command { get; }
    }

    public class MenuBarViewModel
    {
        private readonly ILogger _logger = Logging.Create<MainViewModel>();
        private readonly GameInformationFactory _gameInformationFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly PackFileService _packfileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IUiCommandFactory _uiCommandFactory;

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }
        public ICommand OpenAssetEditorFolderCommand { get; set; }
        public ICommand OpenMountCreatorCommand { get; set; }
        public ICommand OpenAnimationBatchExporterCommand { get; set; }
        public ICommand OpenWh2AnimpackUpdaterCommand { get; set; }
        public ICommand OpenAudioEditorCommand { get; set; }
        public ICommand CompileAudioProjectsCommand { get; set; }
        public ICommand CreateExampleAudioProjectCommand { get; set; }

        public ICommand SearchCommand { get; set; }
        public ICommand OpenRome2RePacksCommand { get; set; }
        public ICommand OpenThreeKingdomsPacksCommand { get; set; }
        public ICommand OpenWarhammer2PacksCommand { get; set; }
        public ICommand OpenWarhammer3PacksCommand { get; set; }
        public ICommand OpenTroyPacksCommand { get; set; }
        public ICommand OpenAttilaPacksCommand { get; set; }

        public ICommand OpenHelpCommand { get; set; }
        public ICommand OpenPatreonCommand { get; set; }
        public ICommand OpenDiscordCommand { get; set; }
        public ICommand DownloadRmeCommand { get; set; }

        public ICommand OpenCampaignAnimCreatorCommand { get; set; }

        public ICommand OpenAnimationTransferToolCommand { get; set; }
        public ICommand OpenSuperViewToolCommand { get; set; }
        public ICommand OpenTechSkeletonEditorCommand { get; set; }

        public ICommand GenerateRmv2ReportCommand { get; set; }
        public ICommand GenerateMetaDataReportCommand { get; set; }
        public ICommand GenerateFileListReportCommand { get; set; }
        public ICommand GenerateMetaDataJsonsReportCommand { get; set; }
        public ICommand CreateAnimPackWarhammer3Command { get; set; }
        public ICommand CreateAnimPack3kCommand { get; set; }
        public ICommand OpenAnimationKeyframeCommand { get; set; }

        // Tutorials
        public ICommand OpenAnimatedPropTutorialCommand { get; set; }
        public ICommand OpenAssetEdBasic0TutorialCommand { get; set; }
        public ICommand OpenAssetEdBasic1TutorialCommand { get; set; }
        public ICommand OpenSkragTutorialCommand { get; set; }
        public ICommand OpenTzarGuardTutorialCommand { get; set; }
        public ICommand OpenKostalynTutorialCommand { get; set; }

        public ObservableCollection<RecentPackFileItem> RecentPackFiles { get; set; } = new ObservableCollection<RecentPackFileItem>();

        public MenuBarViewModel(
            GameInformationFactory gameInformationFactory,
            IServiceProvider provider,
            PackFileService packfileService,
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            ApplicationSettingsService settingsService,
            IUiCommandFactory uiCommandFactory)
        {
            _gameInformationFactory = gameInformationFactory;
            _serviceProvider = provider;
            _packfileService = packfileService;
            _settingsService = settingsService;
            _uiCommandFactory = uiCommandFactory;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;

            OpenSettingsWindowCommand = new RelayCommand(ShowSettingsDialog);
            OpenPackFileCommand = new RelayCommand(OpenPackFile);
            CreateNewPackFileCommand = new RelayCommand(CreatePackFile);
            CreateAnimPackWarhammer3Command = new RelayCommand(CreateAnimationDbWarhammer3);
            CreateAnimPack3kCommand = new RelayCommand(CreateAnimationDb3k);
            OpenAssetEditorFolderCommand = new RelayCommand(OpenAssetEditorFolder);
            OpenMountCreatorCommand = new RelayCommand(OpenMountCreator);
            OpenCampaignAnimCreatorCommand = new RelayCommand(OpenCampaignAnimCreatorEditor);
            OpenAnimationTransferToolCommand = new RelayCommand(OpenAnimationTransferTool);
            OpenSuperViewToolCommand = new RelayCommand(OpenSuperViewTool);
            OpenTechSkeletonEditorCommand = new RelayCommand(OpenTechSkeletonEditor);
            OpenAnimationBatchExporterCommand = new RelayCommand(OpenAnimationBatchExporter);
            OpenWh2AnimpackUpdaterCommand = new RelayCommand(OpenWh2AnimpackUpdater);
            OpenAudioEditorCommand = new RelayCommand(OpenAudioEditor);
            CompileAudioProjectsCommand = new RelayCommand(CompileAudioProjects);
            CreateExampleAudioProjectCommand = new RelayCommand(CreateExampleAudioProject);
            OpenAnimationKeyframeCommand = new RelayCommand(OpenAnimationKeyframe);

            GenerateRmv2ReportCommand = new RelayCommand(GenerateRmv2Report);
            GenerateMetaDataReportCommand = new RelayCommand(GenerateMetaDataReport);
            GenerateFileListReportCommand = new RelayCommand(GenerateFileListReport);
            GenerateMetaDataJsonsReportCommand = new RelayCommand(GenerateMetaDataJsonsReport);

            SearchCommand = new RelayCommand(Search);

            OpenAttilaPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Attila));
            OpenRome2RePacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Rome_2_Remastered));
            OpenThreeKingdomsPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.ThreeKingdoms));
            OpenWarhammer2PacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Warhammer2));
            OpenWarhammer3PacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Warhammer3));
            OpenTroyPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Troy));

            OpenAnimatedPropTutorialCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/watch?v=b68hSHZ5raY") { CreateNoWindow = true }));
            OpenAssetEdBasic0TutorialCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/watch?v=iVjAVEn8jYc") { CreateNoWindow = true }));
            OpenAssetEdBasic1TutorialCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/watch?v=7HN4oA2LsFM") { CreateNoWindow = true }));
            OpenSkragTutorialCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/watch?v=MhvbZfNp8Qw") { CreateNoWindow = true }));
            OpenTzarGuardTutorialCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/watch?v=ONRAKJUmuiM") { CreateNoWindow = true }));
            OpenKostalynTutorialCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/watch?v=AXw99yc74CY") { CreateNoWindow = true }));

            OpenHelpCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://tw-modding.com/index.php/Tutorial:AssetEditor") { CreateNoWindow = true }));
            OpenPatreonCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.patreon.com/TheAssetEditor") { CreateNoWindow = true }));
            OpenDiscordCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://discord.gg/6Djf2sCczC") { CreateNoWindow = true }));
            DownloadRmeCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://github.com/mr-phazer/RME_Release/releases/latest") { CreateNoWindow = true }));

            var settings = settingsService.CurrentSettings;
            settings.RecentPackFilePaths.CollectionChanged += (sender, args) => CreateRecentPackFilesItems();
            CreateRecentPackFilesItems();
        }

        void CreateRecentPackFilesItems()
        {
            var settings = _settingsService.CurrentSettings;

            RecentPackFiles.Clear();
            var menuItemViewModels = settings.RecentPackFilePaths.Select(path => new RecentPackFileItem(
                path,
                () =>
                {
                    if (_packfileService.Load(path, true) == null)
                        System.Windows.MessageBox.Show($"Unable to load packfiles {path}");
                }
            ));
            foreach (var menuItem in menuItemViewModels.Reverse())
            {
                RecentPackFiles.Add(menuItem);
            }
        }

        void OpenPackFile()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Pack files (*.pack)|*.pack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _logger.Here().Information($"Loading pack file {dialog.FileName}");
                if (_packfileService.Load(dialog.FileName, true) == null)
                    System.Windows.MessageBox.Show($"Unable to load packfiles {dialog.FileName}");
            }
        }

        void OpenGamePacks(GameTypeEnum game)
        {
            var settingsService = _serviceProvider.GetService<ApplicationSettingsService>();
            var settings = settingsService.CurrentSettings;
            var gamePath = settings.GameDirectories.FirstOrDefault(x => x.Game == game);
            if (gamePath == null || string.IsNullOrWhiteSpace(gamePath.Path))
            {
                System.Windows.MessageBox.Show("No path provided for game");
                return;
            }
            using (new WaitCursor())
            {
                _packfileService.LoadAllCaFiles(gamePath.Path, _gameInformationFactory.GetGameById(game).DisplayName);
            }
        }

        void ShowSettingsDialog()
        {
            var window = _serviceProvider.GetRequiredService<SettingsWindow>();
            window.DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>();
            window.ShowDialog();
        }

        void CreatePackFile()
        {
            TextInputWindow window = new TextInputWindow("New Packfile name", "");
            if (window.ShowDialog() == true)
            {
                var newPackFile = _packfileService.CreateNewPackFileContainer(window.TextValue, PackFileCAType.MOD);
                _packfileService.SetEditablePack(newPackFile);
            }
        }

        void CreateAnimationDbWarhammer3()
        {
            AnimationPackSampleDataCreator.CreateAnimationDbWarhammer3(_packfileService);
        }

        void CreateAnimationDb3k()
        {
            AnimationPackSampleDataCreator.CreateAnimationDb3k(_packfileService);
        }

        void OpenAssetEditorFolder()
        {
            var path = DirectoryHelper.ApplicationDirectory;
            Process.Start("explorer.exe", path);
        }

        void OpenAnimationKeyframe() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<AnimationKeyframeEditorViewModel>>();
        void OpenMountCreator() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<MountAnimationCreatorViewModel>>();
        void OpenCampaignAnimCreatorEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<CampaignAnimationCreatorViewModel>>();
        void OpenAnimationTransferTool() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<AnimationTransferToolViewModel>>();
        void OpenSuperViewTool() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<SuperViewViewModel>>();
        void OpenAudioEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioEditorViewModel>();
        void CompileAudioProjects() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<CompilerViewModel>();
        void OpenTechSkeletonEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<SkeletonEditorViewModel>>();

        private void CreateExampleAudioProject()
        {
            if (_packfileService.HasEditablePackFile() == false)
                return;

            var pack = _packfileService.GetEditablePack();
            var resourcePath = "AssetEditor.Resources.AudioCompilerExample.json";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            using StreamReader reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            var byteArray = Encoding.ASCII.GetBytes(text);
            _packfileService.AddFileToPack(pack, "AudioProjects", new PackFile("AudioCompilerExample.json", new MemorySource(byteArray)));
        }



        void OpenAnimationBatchExporter() => AnimationBatchExportViewModel.ShowWindow(_packfileService, _skeletonAnimationLookUpHelper);

        void OpenWh2AnimpackUpdater()
        {
            _packfileService.HasEditablePackFile();
            var service = new AnimPackUpdaterService(_packfileService);
            service.Process(_packfileService.GetEditablePack());
        }

        void GenerateRmv2Report()
        {
            var gameName = _gameInformationFactory.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var reportGenerator = new Rmv2ReportGenerator(_packfileService);
            reportGenerator.Create(gameName);
        }

        void GenerateMetaDataReport() => AnimMetaDataReportGenerator.Generate(_packfileService, _settingsService, _gameInformationFactory);

        void GenerateFileListReport() => FileListReportGenerator.Generate(_packfileService, _settingsService, _gameInformationFactory);

        void GenerateMetaDataJsonsReport() => AnimMetaDataJsonsGenerator.Generate(_packfileService, _settingsService, _gameInformationFactory);

        void Search()
        {
            TextInputWindow window = new TextInputWindow("Deep search - Output in console", "");
            if (window.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(window.TextValue))
                {
                    System.Windows.MessageBox.Show("Invalid input");
                    return;
                }
                _packfileService.DeepSearch(window.TextValue, false);
            }
        }
    }
}
