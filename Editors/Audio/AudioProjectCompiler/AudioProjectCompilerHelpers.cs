using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.Core.Misc;

namespace Editors.Audio.AudioProjectCompiler
{
    public class AudioProjectCompilerHelpers
    {
        public static string GetCorrectSoundBankLanguage(string audioProjectLanguage, Wh3SoundBank gameSoundBank)
        {
            // TODO: Add other subtypes as needed
            if (gameSoundBank == Wh3SoundBank.GlobalMusic)
                return Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx);
            else
                return audioProjectLanguage;
        }

        public static void StoreUsedId(HashSet<uint> ids, uint id)
        {
            if (!ids.Add(id))
                return;
        }

        public static List<Sound> GetSounds(AudioProject audioProject)
        {
            var actionEventSounds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents ?? [])
                .SelectMany(actionEvent => actionEvent.GetPlayActions())
                .SelectMany(action => action.Sound != null
                    ? [action.Sound]
                    : action.RandomSequenceContainer?.Sounds ?? [])
                .ToList();

            var dialogueEventSounds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.DialogueEvents ?? [])
                .SelectMany(dialogueEvent => dialogueEvent.StatePaths?
                    .SelectMany(statePath => statePath.Sound != null
                        ? [statePath.Sound]
                        : statePath.RandomSequenceContainer?.Sounds ?? [])
                ?? []);

            return actionEventSounds.Concat(dialogueEventSounds).ToList();
        }

        public static void ClearTempAudioFiles()
        {
            var audioFolder = $"{DirectoryHelper.Temp}\\Audio";
            if (Directory.Exists(audioFolder))
            {
                foreach (var file in Directory.GetFiles(audioFolder, "*.wav"))
                    File.Delete(file);

                foreach (var file in Directory.GetFiles(audioFolder, "*.wem"))
                    File.Delete(file);
            }
        }

        public static ActionEvent GetPlayActionEventFromStopActionEventName(SoundBank soundBank, string stopActionEventName)
        {
            var playActionEventName = string.Concat("Play_", stopActionEventName.AsSpan("Stop_".Length));
            return soundBank.GetActionEvent(playActionEventName);
        }
    }
}
