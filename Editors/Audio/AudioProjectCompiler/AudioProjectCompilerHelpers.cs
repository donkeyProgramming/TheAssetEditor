using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Utility;
using Shared.Core.Misc;

namespace Editors.Audio.AudioProjectCompiler
{
    // TODO check what we don't need here.
    public static class AudioProjectCompilerHelpers
    {
        public static uint GenerateUnusedHircId(Dictionary<uint, List<uint>> usedHircIdsByLanguageLookup, string language)
        {
            var languageId = WwiseHash.Compute(language);
            var usedHircIds = usedHircIdsByLanguageLookup[languageId];
            var unusedHircId = GenerateUnusedId(usedHircIds);
            var index = usedHircIds.BinarySearch(unusedHircId);
            if (index < 0)
                index = ~index;
            usedHircIds.Insert(index, unusedHircId);
            return unusedHircId;
        }

        public static uint GenerateUnusedSourceId(Dictionary<uint, List<uint>> usedSourceIdsByLanguageLookup, string language)
        {
            var languageId = WwiseHash.Compute(language);
            var usedSourceIds = usedSourceIdsByLanguageLookup[languageId];
            var unusedSourceId = GenerateUnusedId(usedSourceIds);
            var index = usedSourceIds.BinarySearch(unusedSourceId);
            if (index < 0)
                index = ~index;
            usedSourceIds.Insert(index, unusedSourceId);
            return unusedSourceId;
        }

        private static uint GenerateUnusedId(List<uint> usedIds)
        {
            uint minId = 1;
            uint maxId = 99999999;

            var usedIdSet = new HashSet<uint>(usedIds);

            for (var candidateId = minId; candidateId <= maxId; candidateId++)
            {
                if (!usedIdSet.Contains(candidateId))
                    return candidateId;
            }

            throw new InvalidOperationException("Houston we have a problem - no unused Ids available.");
        }

        public static string GetCorrectSoundBankLanguage(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                // TODO: Add other subtypes as needed
                if (soundBank.SoundBankSubtype == SoundBanks.Wh3SoundBankSubtype.FrontendMusic)
                    return Languages.Sfx;
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

            // Combine both lists and filter unique sounds based on SourceId.
            var allUniqueSounds = actionSounds
                .Concat(dialogueSounds)
                .DistinctBy(sound => sound.SourceId)
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
