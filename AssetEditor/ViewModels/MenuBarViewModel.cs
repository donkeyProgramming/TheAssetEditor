using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.SuperView;
using AnimationEditor.TechSkeletonEditor;
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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace AssetEditor.ViewModels
{
    public class MenuBarViewModel
    {
        ILogger _logger = Logging.Create<MainViewModel>();

        IServiceProvider _serviceProvider;
        PackFileService _packfileService;
        ToolFactory _toolFactory;

        public ICommand OpenSettingsWindowCommand { get; set; }
        public ICommand CreateNewPackFileCommand { get; set; }
        public ICommand OpenPackFileCommand { get; set; }
        public ICommand OpenAssetEditorFolderCommand { get; set; }
        public ICommand OpenAnimMetaDecocderCommand { get; set; }
        public ICommand OpenMountCreatorCommand { get; set; }
        public ICommand OpenAnimationBatchExporterCommand { get; set; }


        public ICommand OpenRome2RePacksCommand { get; set; }
        public ICommand OpenThreeKingdomsPacksCommand { get; set; }
        public ICommand OpenWarhammer2PacksCommand { get; set; }
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

        public ICommand CreateAnimPackCommand { get; set; }

        public MenuBarViewModel(IServiceProvider provider, PackFileService packfileService, ToolFactory toolFactory)
        {
            _serviceProvider = provider;
            _packfileService = packfileService;
            _toolFactory = toolFactory;
            OpenSettingsWindowCommand = new RelayCommand(ShowSettingsDialog);
            OpenPackFileCommand = new RelayCommand(OpenPackFile);
            CreateNewPackFileCommand = new RelayCommand(CreatePackFile);
            CreateAnimPackCommand = new RelayCommand(CreateAnimPack);
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

            OpenAttilaPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Attila));
            OpenRome2RePacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Rome_2_Remastered));
            OpenThreeKingdomsPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.ThreeKingdoms));
            OpenWarhammer2PacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Warhammer2));
            OpenTroyPacksCommand = new RelayCommand(() => OpenGamePacks(GameTypeEnum.Troy));

            OpenHelpCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://tw-modding.com/index.php/Tutorial:AssetEditor") { CreateNoWindow = true }));
            OpenPatreonCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.patreon.com/TheAssetEditor") { CreateNoWindow = true }));
            OpenDiscordCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://discord.gg/6Djf2sCczC") { CreateNoWindow = true }));
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


        void CreateAnimPack()
        {
            TextInputWindow window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                // Is extention correct
                var fileName = SaveHelper.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/animation_tables/" + fileName;
                var binPath = @"animations/animation_tables/" + SaveHelper.EnsureEnding(fileName, "_tables.bin");
                if (!SaveHelper.IsFilenameUnique(_packfileService, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFile(filePath);
                animPack.AnimationBin = new AnimationBin(binPath);
                animPack.AnimationBin.AnimationTableEntries.Add(
                    new AnimationBinEntry("ExampleDbRef", "ExampleSkeleton")
                    {
                        Unknown = 1,
                        FragmentReferences = new List<AnimationBinEntry.FragmentReference>()
                        {
                            new AnimationBinEntry.FragmentReference() { Name = "FragNameRef0"},
                            new AnimationBinEntry.FragmentReference() { Name = "FragNameRef1"}
                        }
                    });

                // Save
                SaveHelper.Save(_packfileService, filePath, null, animPack.ToByteArray());
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
            var editorView = _toolFactory.CreateEditorViewModel<TechSkeletonEditorViewModel>();
            EditorCreator.CreateEmptyEditor(editorView);
        }

        void OpenAnimationBatchExporter() => AnimationBatchExportViewModel.ShowWindow(_packfileService);

    }
}
