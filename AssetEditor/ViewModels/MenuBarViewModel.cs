using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.SkeletonEditor;
using AssetEditor.UiCommands;
using CommonControls.BaseDialogs;
using CommonControls.Editors.AnimationBatchExporter;
using CommonControls.Editors.AnimationPack;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimationMeta.SuperView;
using Editors.Audio.Presentation.AudioEditor;
using Editors.Audio.Presentation.AudioExplorer;
using Editors.Audio.Presentation.Compiler;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.Events.UiCommands;

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
        private readonly PackFileService _packfileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly TouchedFilesRecorder _touchedFilesRecorder;

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }
        public ICommand OpenAssetEditorFolderCommand { get; set; }
        public ICommand OpenMountCreatorCommand { get; set; }
        public ICommand OpenAnimationBatchExporterCommand { get; set; }
        public ICommand OpenWh2AnimpackUpdaterCommand { get; set; }

        public ICommand OpenAudioExplorerCommand { get; set; }
        public ICommand OpenAudioEditorCommand { get; set; }
        public ICommand CreateTemplateCommand { get; set; }
        public ICommand CompileAudioCommand { get; set; }

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

        public ICommand TouchedFileRecorderStartCommand { get; set; }
        public ICommand TouchedFileRecorderPrintCommand { get; set; }
        public ICommand TouchedFileRecorderExtractCommand { get; set; }
        public ICommand TouchedFileRecorderStopCommand { get; set; }

        public ICommand CreateAnimPackWarhammer3Command { get; set; }
        public ICommand CreateAnimPack3kCommand { get; set; }
        public ICommand OpenAnimationKeyframeCommand { get; set; }

        public ICommand OpenAnimatedPropTutorialCommand { get; set; }
        public ICommand OpenAssetEdBasic0TutorialCommand { get; set; }
        public ICommand OpenAssetEdBasic1TutorialCommand { get; set; }
        public ICommand OpenSkragTutorialCommand { get; set; }
        public ICommand OpenTzarGuardTutorialCommand { get; set; }
        public ICommand OpenKostalynTutorialCommand { get; set; }

        public ObservableCollection<RecentPackFileItem> RecentPackFiles { get; set; } = [];

        public MenuBarViewModel(
            PackFileService packfileService,
            ApplicationSettingsService settingsService,
            IUiCommandFactory uiCommandFactory,
            TouchedFilesRecorder touchedFilesRecorder)
        {
            _packfileService = packfileService;
            _settingsService = settingsService;
            _uiCommandFactory = uiCommandFactory;
            _touchedFilesRecorder = touchedFilesRecorder;

            OpenSettingsWindowCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenSettingsDialogCommand>().Execute());
            OpenPackFileCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenPackFileCommand>().Execute());
            CreateNewPackFileCommand = new RelayCommand(CreatePackFile);
            CreateAnimPackWarhammer3Command = new RelayCommand(() => AnimationPackSampleDataCreator.CreateAnimationDbWarhammer3(_packfileService));
            CreateAnimPack3kCommand = new RelayCommand(() => AnimationPackSampleDataCreator.CreateAnimationDb3k(_packfileService));
            OpenAssetEditorFolderCommand = new RelayCommand(() => Process.Start("explorer.exe", DirectoryHelper.ApplicationDirectory));
            OpenMountCreatorCommand = new RelayCommand(() =>_uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<MountAnimationCreatorViewModel>>());
            OpenCampaignAnimCreatorCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<CampaignAnimationCreatorViewModel>>());
            OpenAnimationTransferToolCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<AnimationTransferToolViewModel>>());
            OpenSuperViewToolCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<SuperViewViewModel>>());
            OpenTechSkeletonEditorCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<SkeletonEditorViewModel>>());
            OpenAnimationBatchExporterCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenAnimationBatchConverterCommand>().Execute());
            OpenWh2AnimpackUpdaterCommand = new RelayCommand(OpenWh2AnimpackUpdater);

            OpenAudioExplorerCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioExplorerViewModel>());
            OpenAudioEditorCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioEditorViewModel>());
            CompileAudioCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<CompilerViewModel>());
            CreateTemplateCommand = new RelayCommand<string>(CreateAudioTemplate);

            OpenAnimationKeyframeCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<AnimationKeyframeEditorViewModel>>());
            GenerateRmv2ReportCommand = new RelayCommand(() => _uiCommandFactory.Create<GenerateReportCommand>().Rmv2());
            GenerateMetaDataReportCommand = new RelayCommand(() => _uiCommandFactory.Create<GenerateReportCommand>().MetaData());
            GenerateFileListReportCommand = new RelayCommand(() => _uiCommandFactory.Create<GenerateReportCommand>().FileList());
            GenerateMetaDataJsonsReportCommand = new RelayCommand(() => _uiCommandFactory.Create<GenerateReportCommand>().MetaDataJson());

            TouchedFileRecorderStartCommand = new RelayCommand(() => _touchedFilesRecorder.Start());
            TouchedFileRecorderPrintCommand = new RelayCommand(() => _touchedFilesRecorder.Print());
            TouchedFileRecorderExtractCommand = new RelayCommand(() => _touchedFilesRecorder.ExtractFilesToPack(@"c:\temp\extractedPack.pack"));
            TouchedFileRecorderStopCommand = new RelayCommand(() => _touchedFilesRecorder.Stop());

            SearchCommand = new RelayCommand(() => _uiCommandFactory.Create<DeepSearchCommand>().Execute());

            OpenAttilaPacksCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Attila));
            OpenRome2RePacksCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Rome_2_Remastered));
            OpenThreeKingdomsPacksCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.ThreeKingdoms));
            OpenWarhammer2PacksCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Warhammer2));
            OpenWarhammer3PacksCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Warhammer3));
            OpenTroyPacksCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Troy));

            OpenAnimatedPropTutorialCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=b68hSHZ5raY"));
            OpenAssetEdBasic0TutorialCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute(" https://www.youtube.com/watch?v=iVjAVEn8jYc"));
            OpenAssetEdBasic1TutorialCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=7HN4oA2LsFM"));
            OpenSkragTutorialCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=MhvbZfNp8Qw"));
            OpenTzarGuardTutorialCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=ONRAKJUmuiM") );
            OpenKostalynTutorialCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.youtube.com/watch?v=AXw99yc74CY"));
            OpenHelpCommand = new RelayCommand(() => _uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://tw-modding.com/index.php/Tutorial:AssetEditor"));
            OpenPatreonCommand = new RelayCommand(() =>_uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://www.patreon.com/TheAssetEditor"));
            OpenDiscordCommand = new RelayCommand(() =>_uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://discord.gg/6Djf2sCczC"));
            DownloadRmeCommand = new RelayCommand(() =>_uiCommandFactory.Create<OpenWebpageCommand>().Execute("https://github.com/mr-phazer/RME_Release/releases/latest"));

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

        void CreatePackFile()
        {
            var window = new TextInputWindow("New Packfile name", "");
            if (window.ShowDialog() == true)
            {
                var newPackFile = _packfileService.CreateNewPackFileContainer(window.TextValue, PackFileCAType.MOD);
                _packfileService.SetEditablePack(newPackFile);
            }
        }


        private void CreateAudioTemplate(string audioTemplateFile)
        {
            if (_packfileService.HasEditablePackFile() == false)
                return;

            var pack = _packfileService.GetEditablePack();
            var resourcePath = $"Shared.EmbeddedResources.Resources.AudioTemplates.{audioTemplateFile}";

            if (resourcePath == null)
                return;

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            var byteArray = Encoding.ASCII.GetBytes(text);
            _packfileService.AddFileToPack(pack, "AudioProjects", new PackFile($"{audioTemplateFile}.json", new MemorySource(byteArray)));
        }

        void OpenWh2AnimpackUpdater()
        {
            _packfileService.HasEditablePackFile();
            var service = new AnimPackUpdaterService(_packfileService);
            service.Process(_packfileService.GetEditablePack());
        }
    }
}
