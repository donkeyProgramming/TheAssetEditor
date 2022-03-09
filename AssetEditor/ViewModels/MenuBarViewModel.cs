using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SuperView;
using AssetEditor.Views.Settings;
using CommonControls.BaseDialogs;
using CommonControls.Common;
using CommonControls.Editors.AnimationBatchExporter;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using AnimationEditor.SkeletonEditor;
using AssetEditor.Report;
using AnimationEditor.AnimationBuilder;

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
        ILogger _logger = Logging.Create<MainViewModel>();

        IServiceProvider _serviceProvider;
        PackFileService _packfileService;
        ToolFactory _toolFactory;
        ApplicationSettingsService _settingsService;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }
        public ICommand OpenAssetEditorFolderCommand { get; set; }
        public ICommand OpenAnimMetaDecocderCommand { get; set; }
        public ICommand OpenMountCreatorCommand { get; set; }
        public ICommand OpenAnimationBatchExporterCommand { get; set; }
        public ICommand OpenAnimationBuilderCommand { get; set; }

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

        public ICommand OpenKitbashEditorCommand { get; set; }
        public ICommand OpenCampaignAnimCreatorCommand { get; set; }
        public ICommand OpenPropCreatorCommand { get; set; }
        public ICommand OpenAnimationTransferToolCommand { get; set; }
        public ICommand OpenSuperViewToolCommand { get; set; }
        public ICommand OpenTechSkeletonEditorCommand { get; set; }
        public IEditorCreator EditorCreator { get; set; }
        public ICommand GenerateRmv2ReportCommand { get; set; }
        public ICommand GenerateMetaDataReportCommand { get; set; }
        public ICommand GenerateFileListReportCommand { get; set; }
        public ICommand CreateAnimPackWarhammerCommand { get; set; }
        public ICommand CreateAnimPackWarhammer3Command { get; set; }
        public ICommand CreateAnimPack3kCommand { get; set; }

        public ObservableCollection<RecentPackFileItem> RecentPackFiles { get; set; } = new ObservableCollection<RecentPackFileItem>();

        public MenuBarViewModel(IServiceProvider provider, PackFileService packfileService, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, ToolFactory toolFactory, ApplicationSettingsService settingsService)
        {
            _serviceProvider = provider;
            _packfileService = packfileService;
            _toolFactory = toolFactory;
            _settingsService = settingsService;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;

            OpenSettingsWindowCommand = new RelayCommand(ShowSettingsDialog);
            OpenPackFileCommand = new RelayCommand(OpenPackFile);
            CreateNewPackFileCommand = new RelayCommand(CreatePackFile);
            CreateAnimPackWarhammerCommand = new RelayCommand(CreateAnimationDbWarhammer);
            CreateAnimPackWarhammer3Command = new RelayCommand(CreateAnimationDbWarhammer3);
            CreateAnimPack3kCommand = new RelayCommand(CreateAnimationDb3k);
            OpenAssetEditorFolderCommand = new RelayCommand(OpenAssetEditorFolder);
            OpenKitbashEditorCommand = new RelayCommand(OpenKitbasherTool);
            OpenAnimMetaDecocderCommand = new RelayCommand(OpenAnimMetaDecocder);
            OpenMountCreatorCommand = new RelayCommand(OpenMountCreator);
            OpenPropCreatorCommand = new RelayCommand(OpenPropCreatorEditor);
            OpenCampaignAnimCreatorCommand = new RelayCommand(OpenCampaignAnimCreatorEditor);
            OpenAnimationTransferToolCommand = new RelayCommand(OpenAnimationTransferTool);
            OpenSuperViewToolCommand = new RelayCommand(OpenSuperViewTool);
            OpenTechSkeletonEditorCommand = new RelayCommand(OpenTechSkeletonEditor);
            OpenAnimationBatchExporterCommand = new RelayCommand(OpenAnimationBatchExporter);
            OpenAnimationBuilderCommand = new RelayCommand(OpenOpenAnimationBuilder);

            GenerateRmv2ReportCommand = new RelayCommand(GenerateRmv2Report);
            GenerateMetaDataReportCommand = new RelayCommand(GenerateMetaDataReport);
            GenerateFileListReportCommand = new RelayCommand(GenerateFileListReport);

            SearchCommand = new RelayCommand(Search);

            OpenAttilaPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Attila));
            OpenRome2RePacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Rome_2_Remastered));
            OpenThreeKingdomsPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.ThreeKingdoms));
            OpenWarhammer2PacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Warhammer2));
            OpenWarhammer3PacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Warhammer3));
            OpenTroyPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Troy));

            OpenHelpCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://tw-modding.com/index.php/Tutorial:AssetEditor") { CreateNoWindow = true }));
            OpenPatreonCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.patreon.com/TheAssetEditor") { CreateNoWindow = true }));
            OpenDiscordCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://discord.gg/6Djf2sCczC") { CreateNoWindow = true }));

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
                        MessageBox.Show($"Unable to load packfiles {path}");
                }
            ));
            foreach (var menuItem in menuItemViewModels.Reverse())
            {
                RecentPackFiles.Add(menuItem);
            }
        }

        void OpenPackFile()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("Pack", ".pack"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _logger.Here().Information($"Loading pack file {dialog.FileName}");
                 if( _packfileService.Load(dialog.FileName, true) == null)
                    MessageBox.Show($"Unable to load packfiles {dialog.FileName}");
            }
        }

        void OpenGamePacks(GameTypeEnum game)
        {
            var settingsService = _serviceProvider.GetService<ApplicationSettingsService>();
            var settings = settingsService.CurrentSettings;
            var gamePath = settings.GameDirectories.FirstOrDefault(x => x.Game == game);
            if (gamePath == null || string.IsNullOrWhiteSpace(gamePath.Path))
            {
                MessageBox.Show("No path provided for game");
                return;
            }
            using (new WaitCursor())
            {
                _packfileService.LoadAllCaFiles(gamePath.Path, GameInformationFactory.GetGameById(game).DisplayName);
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


        void CreateAnimationDb(bool includeWarhamemrBin)
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                // Is extention correct
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/animation_tables/" + fileName;
               
                if (!SaveHelper.IsFilenameUnique(_packfileService, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile();

                var binPath = @"animations/animation_tables/" + SaveHelper.EnsureEnding(fileName, "_tables.bin");
                var animDb = AnimationPackFile.CreateExampleWarhammerBin(binPath);
                animPack.AddFile(animDb);

                // Save
                SaveHelper.Save(_packfileService, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
            }
        }

        void CreateAnimationDbWarhammer()
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/animation_tables/" + fileName;

                if (!SaveHelper.IsFilenameUnique(_packfileService, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile();
                var binPath = @"animations/animation_tables/" + SaveHelper.EnsureEnding(fileName, "_tables.bin");
                var animDb = AnimationPackFile.CreateExampleWarhammerBin(binPath);
                animPack.AddFile(animDb);

                SaveHelper.Save(_packfileService, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
            }
        }


        void CreateAnimationDbWarhammer3()
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/database/battle/bin/" + fileName;

                if (!SaveHelper.IsFilenameUnique(_packfileService, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile();
                var binPath = @"animations/database/battle/bin" + SaveHelper.EnsureEnding(fileName, ".bin");
                var exampleFile = AnimationPackFile.CreateExampleWarhammer3(binPath);
                animPack.AddFile(exampleFile);
                SaveHelper.Save(_packfileService, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
            }
        }

        void CreateAnimationDb3k()
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/database/battle/bin/" + fileName;
                //animations\database\battle\bin\animation_tables.animpack
                if (!SaveHelper.IsFilenameUnique(_packfileService, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile();
                SaveHelper.Save(_packfileService, filePath, null, AnimationPackSerializer.ConvertToBytes(animPack));
            }
        }



        void OpenAssetEditorFolder()
        {
            var path = DirectoryHelper.ApplicationDirectory;
            Process.Start("explorer.exe", path);
        }

        void OpenKitbasherTool()
        {
         //   var editorView = _toolFactory.CreateEdtior<KitbasherEditor.ViewModels.KitbasherViewModel>();
         //   EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenAnimMetaDecocder()
        {
            //var editorView = _toolFactory.CreateEditorViewModel<AnimMetaEditor.ViewModels.MainDecoderViewModel>();
            ////
            // editorView.ConfigureAsDecoder();
            //EditorCreator.CreateEmptyEditor(editorView);
        }
        void OpenPropCreatorEditor()
        {
            var editorView = _toolFactory.CreateEditorViewModel<BaseAnimationViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenMountCreator()
        {
            var editorView = _toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenCampaignAnimCreatorEditor()
        {
            var editorView = _toolFactory.CreateEditorViewModel<CampaignAnimationCreatorViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenAnimationTransferTool()
        {
            var editorView = _toolFactory.CreateEditorViewModel<AnimationTransferToolViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenSuperViewTool()
        {
            var editorView = _toolFactory.CreateEditorViewModel<SuperViewViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }     

        void OpenTechSkeletonEditor()
        {
            var editorView = _toolFactory.CreateEditorViewModel<SkeletonEditorViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenAnimationBatchExporter() => AnimationBatchExportViewModel.ShowWindow(_packfileService, _skeletonAnimationLookUpHelper);

        void OpenOpenAnimationBuilder()
        {
            {
                var editorView = _toolFactory.CreateEditorViewModel<AnimationBuilderViewModel>();
                EditorCreator.CreateEmptyEditor(editorView);
            }
        }

        void GenerateRmv2Report()
        {
            var gameName = GameInformationFactory.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var reportGenerator = new Rmv2ReportGenerator(_packfileService);
            reportGenerator.Create(gameName);
        }

        void GenerateMetaDataReport() => AnimMetaDataReportGenerator.Generate(_packfileService, _settingsService);

        void GenerateFileListReport() => FileListReportGenerator.Generate(_packfileService, _settingsService);


        void Search()
        {
            TextInputWindow window = new TextInputWindow("Deep search - Output in console", "");
            if (window.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(window.TextValue))
                {
                    MessageBox.Show("Invalid input");
                    return;
                }
                _packfileService.DeepSearch(window.TextValue, false);
            }
        }
    }
}
