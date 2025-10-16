using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.Shared.AudioProject;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.Audio.AudioProjectMerger
{
    public partial class AudioProjectMergerViewModel(
        IStandardDialogs standardDialogs,
        IPackFileService packFileService,
        IAudioProjectFileService audioProjectFileService,
        IAudioEditorFileService audioEditorFileService) : ObservableObject
    {
        private readonly IStandardDialogs _standardDialogs = standardDialogs;
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IAudioProjectFileService _audioProjectFileService = audioProjectFileService;
        private readonly IAudioEditorFileService _audioEditorFileService = audioEditorFileService;

        private System.Action _closeAction;

        [ObservableProperty] private string _mergedAudioProjectName;
        [ObservableProperty] private string _outputDirectoryPath;
        [ObservableProperty] private string _baseAudioProjectPath;
        [ObservableProperty] private string _mergingAudioProjectPath;

        [ObservableProperty] private bool _isMergedAudioProjectNameSet;
        [ObservableProperty] private bool _isOutputDirectoryPathSet;
        [ObservableProperty] private bool _isBaseAudioProjectPathSet;
        [ObservableProperty] private bool _isMergingAudioProjectPathSet; 
        [ObservableProperty] private bool _isOkButtonEnabled;

        [RelayCommand] public void MergeAudioProjects()
        {
            var baseAudioProjectFileName = Path.GetFileName(BaseAudioProjectPath);
            var baseAudioProjectPackFile = _packFileService.FindFile(BaseAudioProjectPath);
            var baseAudioProject = _audioProjectFileService.DeserialiseAudioProject(baseAudioProjectPackFile);

            var mergingAudioProjectFileName = Path.GetFileName(MergingAudioProjectPath);
            var mergingAudioProjectPackFile = _packFileService.FindFile(MergingAudioProjectPath);
            var mergingAudioProject = _audioProjectFileService.DeserialiseAudioProject(mergingAudioProjectPackFile);

            baseAudioProject.Merge(mergingAudioProject, baseAudioProjectFileName, mergingAudioProjectFileName);

            var mergedAudioProjectFileName = $"{MergedAudioProjectName}.aproj";
            var mergedAudioProjectFilePath = $"{OutputDirectoryPath}\\{mergedAudioProjectFileName}";
            _audioEditorFileService.Save(baseAudioProject, mergedAudioProjectFileName, mergedAudioProjectFilePath);

            CloseWindowAction();
        }

        partial void OnMergedAudioProjectNameChanged(string value)
        {
            IsMergedAudioProjectNameSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnOutputDirectoryPathChanged(string value)
        {
            IsOutputDirectoryPathSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnBaseAudioProjectPathChanged(string value)
        {
            IsBaseAudioProjectPathSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        partial void OnMergingAudioProjectPathChanged(string value)
        {
            IsMergingAudioProjectPathSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonEnabled = IsMergedAudioProjectNameSet 
                && IsOutputDirectoryPathSet 
                && IsBaseAudioProjectPathSet 
                && IsMergingAudioProjectPathSet;
        }

        [RelayCommand] public void SetOutputDirectoryPath()
        {
            var result = _standardDialogs.DisplayBrowseFolderDialog();
            if (result.Result)
            {
                var filePath = result.Folder;
                OutputDirectoryPath = filePath;
            }
        }

        [RelayCommand] public void SetBaseAudioProjectPath()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".aproj"]);
            if (result.Result)
                BaseAudioProjectPath = _packFileService.GetFullPath(result.File);
        }

        [RelayCommand] public void SetMergingAudioProjectPath()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".aproj"]);
            if (result.Result)
                MergingAudioProjectPath = _packFileService.GetFullPath(result.File);
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();

        public void SetCloseAction(System.Action closeAction) =>_closeAction = closeAction;
    }
}
