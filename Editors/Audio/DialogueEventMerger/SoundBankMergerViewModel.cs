using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.Storage;

namespace Editors.Audio.DialogueEventMerger
{
    public partial class ModdedSoundBank(string filePath, bool isChecked = true) : ObservableObject
    {
        public string FilePath { get; } = filePath;

        [ObservableProperty] private bool _isChecked = isChecked;

        public override string ToString() => FilePath;
    }

    public partial class DialogueEventMergerViewModel : ObservableObject
    {
        private readonly IAudioRepository _audioRepository;
        private readonly ISoundBankGeneratorService _soundBankGeneratorService;

        private System.Action _closeAction;

        [ObservableProperty] private string _soundBankSuffix;
        [ObservableProperty] private bool _isSoundBankSuffixSet;
        [ObservableProperty] private bool _isOkButtonEnabled;
        [ObservableProperty] private ObservableCollection<string> _selectedModdedSoundBanks = [];
        public ObservableCollection<ModdedSoundBank> ModdedSoundBanks { get; }

        public DialogueEventMergerViewModel(IAudioRepository audioRepository, ISoundBankGeneratorService soundBankGeneratorService)
        {
            _audioRepository = audioRepository;
            _soundBankGeneratorService = soundBankGeneratorService;

            ModdedSoundBanks = new ObservableCollection<ModdedSoundBank>(_audioRepository.GetModdedSoundBankFilePaths("for_merging")
                .Select(path => new ModdedSoundBank(path, isChecked: true))
            );

            SelectedModdedSoundBanks = new ObservableCollection<string>(ModdedSoundBanks.Select(x => x.FilePath));
        }

        partial void OnSoundBankSuffixChanged(string value)
        {
            IsSoundBankSuffixSet = !string.IsNullOrEmpty(value);
            UpdateOkButtonIsEnabled();
        }

        private void UpdateOkButtonIsEnabled()
        {
            IsOkButtonEnabled = IsSoundBankSuffixSet && SelectedModdedSoundBanks.Any();
        }

        [RelayCommand] private void ModdedSoundBanksSelectionChanged(IList selectedItems)
        {
            SelectedModdedSoundBanks = new ObservableCollection<string>(selectedItems.Cast<string>());

            UpdateOkButtonIsEnabled();
        }

        [RelayCommand] public void GenerateMergedDialogueEventsSoundBank()
        {
            _soundBankGeneratorService.GenerateMergedDialogueEventSoundBanks(SelectedModdedSoundBanks.ToList());

            CloseWindowAction();
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();

        public void SetCloseAction(System.Action closeAction) => _closeAction = closeAction;
    }
}
