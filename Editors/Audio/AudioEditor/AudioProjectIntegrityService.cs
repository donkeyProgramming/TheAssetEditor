using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioProjectIntegrityService
    {
        void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> dialogueEventData);
        void CheckAudioProjectDialogueEventIntegrity(AudioProject audioProject);
        void CheckAudioProjectWavFilesIntegrity(AudioProject audioProject);
    }

    public class AudioProjectIntegrityService(IPackFileService packFileService, IAudioRepository audioRepository) : IAudioProjectIntegrityService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IAudioRepository _audioRepository = audioRepository;

        public void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> information)
        {
            var exclusions = new List<string> { "New_Dialogue_Event", "Battle_Individual_Melee_Weapon_Hit" };
            var gameDialogueEvents = _audioRepository.StateGroupsByDialogueEvent.Keys.Except(exclusions).ToList();
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
                            var gameDialogueEventStateGroups = _audioRepository.StateGroupsByDialogueEvent[dialogueEvent.Name];
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

        public void CheckAudioProjectWavFilesIntegrity(AudioProject audioProject)
        {
            var missingWavFiles = new List<string>();
            var sounds = audioProject.GetSounds();
            var distinctSounds = sounds.DistinctBy(sound => sound.WavPackFilePath);
            foreach (var sound in distinctSounds)
            {
                var wavPath = sound.WavPackFilePath;
                if (string.IsNullOrWhiteSpace(wavPath))
                    continue;

                var packFile = _packFileService.FindFile(wavPath);
                if (packFile == null)
                    missingWavFiles.Add(wavPath);
            }

            if (missingWavFiles.Count > 0)
            {
                var sortedMissingWavFiles = missingWavFiles.OrderBy(wavFilePath => wavFilePath).ToList();
                var missingWavFilesText = string.Join("\n - ", sortedMissingWavFiles);

                var message =
                    $"Wav files integrity check failed." +
                    $"\n\nThe following wav files could not be found:" +
                    $"\n - {missingWavFilesText}" +
                    $"\n\nEnsure all wav files are in the correct location or update their usage in the Audio Project to the correct path.";

                MessageBox.Show(message, "Error");
            }
        }
    }
}
