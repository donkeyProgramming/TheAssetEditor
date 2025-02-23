using System;
using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler
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

        public static string GetCorrectSoundBankLanguage(AudioProjectDataModel audioProject)
        {
            // TODO: implement game version into this check
            foreach (var soundBank in audioProject.SoundBanks)
            {
                //if (soundBank.SoundBankSubType == SoundBanks.Wh3SoundBankSubType.BattleIndividualMelee) // TODO: This isn't necessarily the right subtype, get the right ones
                    //return Languages.Sfx;
            }

            return audioProject.Language;
        }
    }
}
