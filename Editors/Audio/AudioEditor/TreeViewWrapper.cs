using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProject;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor
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

    public class TreeViewWrapper
    {
        public static void AddAllSoundBanksToTreeViewItemsWrappers(IAudioProjectService audioProjectService)
        {
            audioProjectService.AudioProject.AudioProjectTreeViewItems.Clear();

            var actionEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString())
                .ToList();

            if (actionEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new ActionEventSoundBanksTreeViewWrapper
                {
                    ActionEventSoundBanks = new ObservableCollection<SoundBank>(actionEventSoundBanks)
                });
            }

            var dialogueEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString())
                .ToList();

            if (dialogueEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new DialogueEventSoundBanksTreeViewWrapper
                {
                    DialogueEventSoundBanks = new ObservableCollection<SoundBank>(dialogueEventSoundBanks)
                });
            }

            var musicEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.MusicEventSoundBank.ToString())
                .ToList();

            /*
            if (musicEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new MusicEventSoundBanksTreeViewWrapper
                {
                    MusicEventSoundBanks = new ObservableCollection<SoundBank>(musicEventSoundBanks)
                });
            }
            */

            if (audioProjectService.AudioProject.ModdedStates.Any())
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new ModdedStatesTreeViewWrapper
                {
                    ModdedStates = audioProjectService.AudioProject.ModdedStates
                });
            }
        }

        public static void AddEditedSoundBanksToAudioProjectTreeViewItemsWrappers(IAudioProjectService audioProjectService)
        {
            audioProjectService.AudioProject.AudioProjectTreeViewItems.Clear();

            var actionEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.ActionEventSoundBank.ToString()
                    && soundBank.ActionEvents.Count > 0)
                .ToList();

            if (actionEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new ActionEventSoundBanksTreeViewWrapper
                {
                    ActionEventSoundBanks = new ObservableCollection<SoundBank>(actionEventSoundBanks)
                });
            }

            var dialogueEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.DialogueEventSoundBank.ToString()
                    && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.DecisionTree.Count > 0))
                .ToList();

            if (dialogueEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new DialogueEventSoundBanksTreeViewWrapper
                {
                    DialogueEventSoundBanks = new ObservableCollection<SoundBank>(dialogueEventSoundBanks)
                });
            }

            var musicEventSoundBanks = audioProjectService.AudioProject.SoundBanks
                .Where(soundBank => soundBank.Type == GameSoundBankType.MusicEventSoundBank.ToString()
                    && soundBank.MusicEvents.Count > 0)
                .ToList();

            /*
            if (musicEventSoundBanks.Count != 0)
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new MusicEventSoundBanksTreeViewWrapper
                {
                    MusicEventSoundBanks = new ObservableCollection<SoundBank>(musicEventSoundBanks)
                });
            }
            */

            if (audioProjectService.AudioProject.ModdedStates.Any())
            {
                audioProjectService.AudioProject.AudioProjectTreeViewItems.Add(new ModdedStatesTreeViewWrapper
                {
                    ModdedStates = audioProjectService.AudioProject.ModdedStates
                });
            }
        }
    }
}
