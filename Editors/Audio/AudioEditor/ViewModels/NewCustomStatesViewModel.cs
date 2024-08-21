using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommonControls.PackFileBrowser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using static Editors.Audio.AudioEditor.AudioEditorData;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.AudioProjectData;
using static Editors.Audio.AudioEditor.SettingsEnumConverter;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public partial class NewCustomStatesViewModel : ObservableObject, IEditorViewModel
    {
        private readonly IAudioRepository _audioRepository;
        private readonly PackFileService _packFileService;
        private readonly AudioEditorViewModel _audioEditorViewModel;
        //readonly ILogger _logger = Logging.Create<NewCustomStatesViewModel>();
        private Action _closeAction;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("New Custom States");

        // The properties for each settings.
        [ObservableProperty] private string _customStatesFileName;

        // Properties to control whether OK button is enabled.
        [ObservableProperty] private bool _isCustomStatesFileNameSet;
        [ObservableProperty] private bool _isOkButtonIsEnabled;

        public NewCustomStatesViewModel(IAudioRepository audioRepository, PackFileService packFileService, AudioEditorViewModel audioEditorViewModel)
        {
            _audioRepository = audioRepository;
            _packFileService = packFileService;
            _audioEditorViewModel = audioEditorViewModel;
        }
 
        public void ResetNewCustomStatesViewModelData()
        {
        }

        [RelayCommand] public void CloseWindowAction()
        {
            _closeAction?.Invoke();

            ResetNewCustomStatesViewModelData();
        }

        public void SetCloseAction(Action closeAction)
        {
            _closeAction = closeAction;
        }

        public void Close()
        {
        }

        public bool Save() => true;

        public PackFile MainFile { get; set; }

        public bool HasUnsavedChanges { get; set; } = false;
    }
}
