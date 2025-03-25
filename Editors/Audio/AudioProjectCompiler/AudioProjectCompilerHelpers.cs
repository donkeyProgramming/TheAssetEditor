using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.Utility;
using Shared.Core.Misc;

namespace Editors.Audio.AudioProjectCompiler
{
    public static class AudioProjectCompilerHelpers
    {
        public static uint GenerateUnusedHircID(Dictionary<uint, List<uint>> usedHircIdsByLanguageLookup, string language)
        {
            var languageID = WwiseHash.Compute(language);
            var usedHircIds = usedHircIdsByLanguageLookup[languageID];
            var unusedHircID = GenerateUnusedID(usedHircIds);
            var index = usedHircIds.BinarySearch(unusedHircID);
            if (index < 0)
                index = ~index;
            usedHircIds.Insert(index, unusedHircID);
            return unusedHircID;
        }

        public static uint GenerateUnusedSourceID(Dictionary<uint, List<uint>> usedSourceIdsByLanguageLookup, string language)
        {
            var languageID = WwiseHash.Compute(language);
            var usedSourceIds = usedSourceIdsByLanguageLookup[languageID];
            var unusedSourceID = GenerateUnusedID(usedSourceIds);
            var index = usedSourceIds.BinarySearch(unusedSourceID);
            if (index < 0)
                index = ~index;
            usedSourceIds.Insert(index, unusedSourceID);
            return unusedSourceID;
        }

        private static uint GenerateUnusedID(List<uint> usedIds)
        {
            uint minID = 1;
            uint maxID = 99999999;

            var usedIdSet = new HashSet<uint>(usedIds);

            for (var candidateID = minID; candidateID <= maxID; candidateID++)
            {
                if (!usedIdSet.Contains(candidateID))
                    return candidateID;
            }

            throw new InvalidOperationException("Houston we have a problem - no unused IDs available.");
        }

        public static string GetCorrectSoundBankLanguage(AudioProject audioProject)
        {
            // TODO: implement game version into this check
            foreach (var soundBank in audioProject.SoundBanks)
            {
                //if (soundBank.SoundBankSubType == SoundBanks.Wh3SoundBankSubtype.BattleIndividualMelee) // TODO: This isn't necessarily the right subtype, get the right ones
                    //return Languages.Sfx;
            }

            return audioProject.Language;
        }

        public static List<Sound> GetAllUniqueSounds(AudioProject audioProject)
        {
            // Extract sounds from ActionEvents if available.
            var actionSounds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents ?? Enumerable.Empty<ActionEvent>())
                .SelectMany(actionEvent =>
                    actionEvent.Sound != null
                        ? [actionEvent.Sound]
                        : actionEvent.RandomSequenceContainer?.Sounds ?? Enumerable.Empty<Sound>());

            // Extract sounds from DialogueEvents if available.
            var dialogueSounds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.DialogueEvents ?? Enumerable.Empty<DialogueEvent>())
                .SelectMany(dialogueEvent =>
                    dialogueEvent.StatePaths?.SelectMany(statePath =>
                        statePath.Sound != null
                            ? [statePath.Sound]
                            : statePath.RandomSequenceContainer?.Sounds ?? Enumerable.Empty<Sound>())
                    ?? Enumerable.Empty<Sound>());

            // Combine both lists and filter unique sounds based on SourceID.
            var allUniqueSounds = actionSounds
                .Concat(dialogueSounds)
                .DistinctBy(sound => sound.SourceID)
                .ToList();

            return allUniqueSounds;
        }

        public static void DeleteAudioFilesInTempAudioFolder()
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
    }
}
