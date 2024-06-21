using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Editors.Audio.Presentation.AudioEditor.ViewModels
{
    public enum AudioProjectType
    {
        None,
        CampaignVO,
        BattleVO,
        FrontendVO
    }

    public enum DialogueEventsPreset
    {
        None,
        Essential,
        All
    }

    public enum VOType
    {
        None,
        CampaignLord,
        CampaignHero,
        MeleeLord,
        SkirmishLord,
        CasterLord,
        MeleeHero,
        SkirmishHero,
        CasterHero,
        InfantryUnit,
        SkirmisherUnit,
        CavalryUnit,
        SEMUnit,
        ArtilleryUnit,
        GenericFrontend
    }

    public class AudioEditorSettingsViewModel : INotifyPropertyChanged
    {
        private AudioProjectType _selectedProjectType = AudioProjectType.None;
        public AudioProjectType SelectedProjectType
        {
            get { return _selectedProjectType; }
            set
            {
                if (_selectedProjectType != value)
                {
                    _selectedProjectType = value;
                    OnPropertyChanged(nameof(SelectedProjectType));
                    OnPropertyChanged(nameof(SelectedProjectVisibility));
                }
            }
        }

        private VOType _selectedVOType = VOType.None;
        public VOType SelectedVOType
        {
            get { return _selectedVOType; }
            set
            {
                if (_selectedVOType != value)
                {
                    _selectedVOType = value;
                    OnPropertyChanged(nameof(SelectedVOType));
                }
            }
        }

        private DialogueEventsPreset _selectedDialogueEventsPreset = DialogueEventsPreset.None;
        public DialogueEventsPreset SelectedDialogueEventsPreset
        {
            get { return _selectedDialogueEventsPreset; }
            set
            {
                if (_selectedDialogueEventsPreset != value)
                {
                    _selectedDialogueEventsPreset = value;
                    OnPropertyChanged(nameof(SelectedDialogueEventsPreset));
                }
            }
        }

        public Visibility SelectedProjectVisibility
        {
            get
            {
                switch (SelectedProjectType)
                {
                    case AudioProjectType.CampaignVO:
                        return Visibility.Visible;
                    case AudioProjectType.BattleVO:
                        return Visibility.Collapsed;
                    case AudioProjectType.FrontendVO:
                        return Visibility.Collapsed; 
                    default:
                        return Visibility.Collapsed;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            var propertyInfo = GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                Debug.WriteLine($"{propertyName} changed to: {propertyInfo.GetValue(this)}");
            }
        }
    }
}
