using System;
using System.Collections.ObjectModel;
using System.Linq;
using Shared.Core.ErrorHandling;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

namespace Editors.Audio.AudioEditor.ViewModels
{
    public class ActionEventSoundBanksTreeViewWrapper
    {
        public ObservableCollection<SoundBank> ActionEventSoundBanks { get; set; }
        public static string Name => "Action Events";
    }

    public class DialogueEventSoundBanksTreeViewWrapper
    {
        public ObservableCollection<SoundBank> DialogueEventSoundBanks { get; set; }
        public static string Name => "Dialogue Events";
    }

    public class MusicEventSoundBanksTreeViewWrapper
    {
        public ObservableCollection<SoundBank> MusicEventSoundBanks { get; set; }
        public static string Name => "Music Events";
    }

    public class ModdedStatesTreeViewWrapper
    {
        public ObservableCollection<StateGroup> ModdedStates { get; set; }
        public static string Name => "States";
    }

    public partial class AudioEditorViewModel
    {
        public void UpdateAudioProjectTreeViewItems()
        {
            _audioProjectService.AudioProject.AudioProjectTreeViewItems.Clear();

            var actionEventSoundBanks = _audioProjectService.AudioProject.SoundBanks.Where(soundBank => soundBank.Type == SoundBankType.ActionEventBnk.ToString()).ToList();

            if (actionEventSoundBanks.Count != 0)
                AudioProjectTreeViewItems.Add(new ActionEventSoundBanksTreeViewWrapper { ActionEventSoundBanks = new ObservableCollection<SoundBank>(actionEventSoundBanks) });

            var dialogueEventSoundBanks = _audioProjectService.AudioProject.SoundBanks.Where(soundBank => soundBank.Type == SoundBankType.DialogueEventBnk.ToString()).ToList();

            if (dialogueEventSoundBanks.Count != 0)
                AudioProjectTreeViewItems.Add(new DialogueEventSoundBanksTreeViewWrapper { DialogueEventSoundBanks = new ObservableCollection<SoundBank>(dialogueEventSoundBanks) });

            var musicEventSoundBanks = _audioProjectService.AudioProject.SoundBanks.Where(soundBank => soundBank.Type == SoundBankType.MusicEventBnk.ToString()).ToList();

            if (musicEventSoundBanks.Count != 0)
                AudioProjectTreeViewItems.Add(new MusicEventSoundBanksTreeViewWrapper { MusicEventSoundBanks = new ObservableCollection<SoundBank>(musicEventSoundBanks) });

            if (_audioProjectService.AudioProject.ModdedStates.Any())
                AudioProjectTreeViewItems.Add(new ModdedStatesTreeViewWrapper { ModdedStates = _audioProjectService.AudioProject.ModdedStates });
        }

        public void OnSelectedAudioProjectEventChanged(object selectedAudioProjectTreeViewItem)
        {
            if (_selectedAudioProjectTreeItem != null)
                _previousSelectedAudioProjectTreeItem = _selectedAudioProjectTreeItem;

            _selectedAudioProjectTreeItem = selectedAudioProjectTreeViewItem;

            if (_selectedAudioProjectTreeItem is SoundBank selectedSoundBank)
            {
                if (selectedSoundBank.Type == SoundBankType.ActionEventBnk.ToString())
                {
                    LoadActionEventSoundBankForAudioProjectEditor(selectedSoundBank);
                    LoadActionEventSoundBankForAudioProjectViewer(selectedSoundBank);

                    _logger.Here().Information($"Loaded Action Event SoundBank: {selectedSoundBank.Name}");
                }

                else if (selectedSoundBank.Type == SoundBankType.MusicEventBnk.ToString())
                {
                    throw new NotImplementedException();
                }
            }

            else if (_selectedAudioProjectTreeItem is DialogueEvent selectedDialogueEvent)
            {
                if (_audioProjectService.StateGroupsWithCustomStates == null || _audioProjectService.StateGroupsWithCustomStates.Count == 0)
                    IsShowModdedStatesCheckBoxEnabled = true;

                var areStateGroupsEqual = false;

                if (_previousSelectedAudioProjectTreeItem is DialogueEvent previousSelectedDialogueEvent)
                {
                    var newEventStateGroups = _audioRepository.DialogueEventsWithStateGroups[selectedDialogueEvent.Name];
                    var oldEventStateGroups = _audioRepository.DialogueEventsWithStateGroups[previousSelectedDialogueEvent.Name];
                    areStateGroupsEqual = newEventStateGroups.SequenceEqual(oldEventStateGroups);
                }

                GetModdedStates(_audioProjectService.AudioProject.ModdedStates, _audioProjectService.StateGroupsWithCustomStates);

                LoadDialogueEventForAudioProjectEditor(selectedDialogueEvent, ShowModdedStatesOnly, areStateGroupsEqual);
                LoadDialogueEventForAudioProjectViewer(selectedDialogueEvent, ShowModdedStatesOnly, areStateGroupsEqual);

                _logger.Here().Information($"Loaded DialogueEvent: {selectedDialogueEvent.Name}");
            }

            else if (_selectedAudioProjectTreeItem is StateGroup selectedStateGroup)
            {
                var stateGroupWithExtraUnderscores = AddExtraUnderscoresToString(selectedStateGroup.Name);

                LoadStateGroupForAudioProjectEditor(selectedStateGroup, stateGroupWithExtraUnderscores);
                LoadStateGroupForAudioProjectViewer(selectedStateGroup, stateGroupWithExtraUnderscores);

                _logger.Here().Information($"Loaded Events ModdedStateGroup>: {selectedStateGroup.Name}");
            }

            SetIsPasteEnabled();
        }
    }
}
