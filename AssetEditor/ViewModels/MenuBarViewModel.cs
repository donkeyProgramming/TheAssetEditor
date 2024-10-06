﻿using System;
using System.CodeDom.Compiler;
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
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.AudioExplorer;
using Editors.Audio.Compiler;
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

    public partial class MenuBarViewModel
    {
        private readonly PackFileService _packfileService;
        private readonly ApplicationSettingsService _settingsService;
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly TouchedFilesRecorder _touchedFilesRecorder;


        public ObservableCollection<RecentPackFileItem> RecentPackFiles { get; set; } = [];

        public MenuBarViewModel(PackFileService packfileService, ApplicationSettingsService settingsService, IUiCommandFactory uiCommandFactory, TouchedFilesRecorder touchedFilesRecorder)
        {
            _packfileService = packfileService;
            _settingsService = settingsService;
            _uiCommandFactory = uiCommandFactory;
            _touchedFilesRecorder = touchedFilesRecorder;

            var settings = settingsService.CurrentSettings;
            settings.RecentPackFilePaths.CollectionChanged += (sender, args) => CreateRecentPackFilesItems();
            CreateRecentPackFilesItems();
        }

        [RelayCommand] private void OpenSettingsWindow() => _uiCommandFactory.Create<OpenSettingsDialogCommand>().Execute();
        [RelayCommand] private void OpenPackFile() => _uiCommandFactory.Create<OpenPackFileCommand>().Execute();
        [RelayCommand] private void CreateNewPackFile()
        {
            var window = new TextInputWindow("New Pack Name", "");
            if (window.ShowDialog() == true)
            {
                var newPackFile = _packfileService.CreateNewPackFileContainer(window.TextValue, PackFileCAType.MOD);
                _packfileService.SetEditablePack(newPackFile);
            }
        }
        [RelayCommand] private void CreateAnimPackWarhammer3() => AnimationPackSampleDataCreator.CreateAnimationDbWarhammer3(_packfileService);
        [RelayCommand] private void CreateAnimPack3k() => AnimationPackSampleDataCreator.CreateAnimationDb3k(_packfileService);
        [RelayCommand] private void OpenMountCreator() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<MountAnimationCreatorViewModel>>();
        [RelayCommand] private void OpenCampaignAnimCreator() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<CampaignAnimationCreatorViewModel>>();
        [RelayCommand] private void OpenAnimationTransferTool() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<AnimationTransferToolViewModel>>();
        [RelayCommand] private void OpenSuperViewTool() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<SuperViewViewModel>>();
        [RelayCommand] private void OpenTechSkeletonEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<SkeletonEditorViewModel>>();
        [RelayCommand] private void OpenAnimationBatchExporter() => _uiCommandFactory.Create<OpenAnimationBatchConverterCommand>().Execute();
        [RelayCommand] private void OpenWh2AnimpackUpdater()
        {
            _packfileService.HasEditablePackFile();
            var service = new AnimPackUpdaterService(_packfileService);
            service.Process(_packfileService.GetEditablePack());
        }

        [RelayCommand] private void OpenAudioExplorer() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioExplorerViewModel>();
        [RelayCommand] private void OpenAudioEditor() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioEditorViewModel>();
        [RelayCommand] private void CompileAudio() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<CompilerViewModel>();

        [RelayCommand] private void OpenAnimationKeyframe() => _uiCommandFactory.Create<OpenEditorCommand>().Execute<EditorHost<AnimationKeyframeEditorViewModel>>();
        [RelayCommand] private void GenerateRmv2Report() => _uiCommandFactory.Create<GenerateReportCommand>().Rmv2();
        [RelayCommand] private void GenerateMetaDataReport() => _uiCommandFactory.Create<GenerateReportCommand>().MetaData();
        [RelayCommand] private void GenerateFileListReport() => _uiCommandFactory.Create<GenerateReportCommand>().FileList();
        [RelayCommand] private void GenerateMetaDataJsonsReport() => _uiCommandFactory.Create<GenerateReportCommand>().MetaDataJson();
        [RelayCommand] private void GenerateMaterialReport() => _uiCommandFactory.Create<GenerateReportCommand>().Material();

        [RelayCommand] private void TouchedFileRecorderStart() => _touchedFilesRecorder.Start();
        [RelayCommand] private void TouchedFileRecorderPrint() => _touchedFilesRecorder.Print();
        [RelayCommand] private void TouchedFileRecorderExtract() => _touchedFilesRecorder.ExtractFilesToPack(@"c:\temp\extractedPack.pack");
        [RelayCommand] private void TouchedFileRecorderStop() => _touchedFilesRecorder.Stop();
        [RelayCommand] private void Search() => _uiCommandFactory.Create<DeepSearchCommand>().Execute();

        [RelayCommand] private void OpenAttilaPacks() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.Attila);
        [RelayCommand] private void OpenRome2RePacks() => _uiCommandFactory.Create<OpenGamePackCommand>().Execute(GameTypeEnum.RomeRemastered);
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

        [RelayCommand] private void OpenAssetEditorFolder() => Process.Start("explorer.exe", DirectoryHelper.ApplicationDirectory);

        [RelayCommand] private void ClearAssetEditorFolder()
        {
            try {Directory.Delete(DirectoryHelper.ApplicationDirectory, true);} catch {}
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
    }
}
