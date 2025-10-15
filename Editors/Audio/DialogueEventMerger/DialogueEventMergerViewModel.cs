using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.Generators;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Editors.Audio.DialogueEventMerger
{
    public partial class DialogueEventMergerViewModel : ObservableObject
    {
        private readonly IAudioRepository _audioRepository;
        private readonly ISoundBankGeneratorService _soundBankGeneratorService;

        private readonly ILogger _logger = Logging.Create<AudioProjectCompilerService>();
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

            _audioRepository.Load(Wh3LanguageInformation.GetAllLanguages());

            ModdedSoundBanks = new ObservableCollection<ModdedSoundBank>(_audioRepository.GetModdedSoundBankFilePaths("for_merging")
                .Select(path => new ModdedSoundBank(path, isChecked: true))
            );

            ModdedSoundBanks.CollectionChanged += OnModdedSoundBanksCollectionChanged;
            foreach (var item in ModdedSoundBanks)
                item.PropertyChanged += OnModdedSoundBankPropertyChanged;

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

        private void OnModdedSoundBanksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ModdedSoundBank item in e.NewItems)
                    item.PropertyChanged += OnModdedSoundBankPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (ModdedSoundBank item in e.OldItems)
                    item.PropertyChanged -= OnModdedSoundBankPropertyChanged;
            }

            SetSelectedModdedSoundBanks();
        }

        private void OnModdedSoundBankPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModdedSoundBank.IsChecked))
                SetSelectedModdedSoundBanks();
        }

        private void SetSelectedModdedSoundBanks()
        {
            SelectedModdedSoundBanks = new ObservableCollection<string>(ModdedSoundBanks.Where(x => x.IsChecked).Select(x => x.FilePath));
            UpdateOkButtonIsEnabled();
        }

        [RelayCommand] public void GenerateMergedDialogueEventSoundBank()
        {
            _logger.Here().Information($"Generating merged Dialogue Event SoundBanks");

            _soundBankGeneratorService.GenerateMergedDialogueEventSoundBanks(SelectedModdedSoundBanks.ToList(), SoundBankSuffix);

            CloseWindowAction();
        }

        [RelayCommand] public void CloseWindowAction() => _closeAction?.Invoke();

        public void SetCloseAction(System.Action closeAction) => _closeAction = closeAction;
    }
}
