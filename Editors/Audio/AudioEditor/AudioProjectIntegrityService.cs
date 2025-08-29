using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor
{
    // TODO: Probably need something to check all audio files in the project are where they say they are
    public interface IAudioProjectIntegrityService
    {
        void CheckAudioProjectDialogueEventIntegrity(AudioProject audioProject);

        void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> dialogueEventData);
    }

    public class AudioProjectIntegrityService(IAudioRepository audioRepository) : IAudioProjectIntegrityService
    {
        private readonly IAudioRepository _audioRepository = audioRepository;

        public void CheckAudioProjectDialogueEventIntegrity(AudioProject audioProject)
        {
            var audioProjectDialogueEventsWithStateGroups = new Dictionary<string, List<string>>();
            var dialogueEventsWithStateGroupsWithIntegrityError = new Dictionary<string, List<string>>();
            var hasIntegrityError = false;

            var message = $"Dialogue Events State Groups integrity check failed." +
                $"\n\nThis is likely due to a change in the State Groups by a recent update to the game or you've done something silly with the file." +
                $"\n\nWhen browsing the affected Dialogue Events you will see the rows have been updated to accommodate for the new State Groups, and if any of the old State Groups are no longer used they will have been removed." +
                $" The new State Group(s) will have no State set in them so you need to click Update Row and add the State(s). " +
                $"\n\nAffected Dialogue Events:";

            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank))
                {
                    foreach (var dialogueEvent in soundBank.DialogueEvents)
                    {
                        var firstStatePath = dialogueEvent.StatePaths.FirstOrDefault();
                        if (firstStatePath != null)
                        {
                            var gameDialogueEventStateGroups = _audioRepository.StateGroupsLookupByDialogueEvent[dialogueEvent.Name];
                            var audioProjectDialogueEventStateGroups = firstStatePath.Nodes.Select(node => node.StateGroup.Name).ToList();
                            if (!gameDialogueEventStateGroups.SequenceEqual(audioProjectDialogueEventStateGroups))
                            {
                                if (!hasIntegrityError)
                                    hasIntegrityError = true;

                                message += $"\n\nDialogue Event: {dialogueEvent.Name}";

                                var audioProjectDialogueEventStateGroupsText = string.Join("-->", audioProjectDialogueEventStateGroups);
                                message += $"\n\nOld State Groups: {audioProjectDialogueEventStateGroupsText}";

                                var gameDialogueEventStateGroupsText = string.Join("-->", gameDialogueEventStateGroups);
                                message += $"\n\nNew State Groups: {gameDialogueEventStateGroupsText}";
                            }
                        }
                    }
                }
            }

            if (hasIntegrityError)
                MessageBox.Show(message, "Error");
        }

        public void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> information)
        {
            var exclusions = new List<string>{"New_Dialogue_Event", "Battle_Individual_Melee_Weapon_Hit"};
            var gameDialogueEvents = _audioRepository.StateGroupsLookupByDialogueEvent.Keys.Except(exclusions).ToList();
            var audioEditorDialogueEvents = information.Select(item => item.Name).ToList();
            var areGameAndAudioEditorDialogueEventsMatching = new HashSet<string>(gameDialogueEvents).SetEquals(audioEditorDialogueEvents);
            if (!areGameAndAudioEditorDialogueEventsMatching)
            {
                var dialogueEventsOnlyInGame = gameDialogueEvents.Except(audioEditorDialogueEvents).ToList();
                var dialogueEventsOnlyInAudioEditor = audioEditorDialogueEvents.Except(gameDialogueEvents).ToList();

                var dialogueEventsOnlyInGameText = string.Join("\n - ", dialogueEventsOnlyInGame);
                var dialogueEventsOnlyInAudioEditorText = string.Join("\n - ", dialogueEventsOnlyInAudioEditor);

                var message = $"Dialogue Event integrity check failed." +
                    $"\n\nThis is due to a change in the game's Dialogue Events by CA." +
                    $"\n\nPlease report this error to the AssetEditor development team.";
                var dialogueEventsOnlyInGameMessage = $"\n\nGame Dialogue Events not in the Audio Editor:\n - {dialogueEventsOnlyInGameText}";
                var dialogueEventsOnlyInAudioEditorMessage = $"\n\nAudio Editor Dialogue Events not in the game:\n - {dialogueEventsOnlyInAudioEditorText}";

                if (dialogueEventsOnlyInGame.Count > 0)
                    message += dialogueEventsOnlyInGameMessage;

                if (dialogueEventsOnlyInAudioEditor.Count > 0)
                    message += dialogueEventsOnlyInAudioEditorMessage;

                MessageBox.Show(message, "Error");
            }
        }
    }
}
