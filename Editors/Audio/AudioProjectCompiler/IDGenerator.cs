using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioProjectCompiler
{
    public static class IdGenerator
    {
        public record Result(uint Id, string FinalKey, string OriginalKey, int Collisions);

        public static Result GenerateUniqueId(HashSet<uint> usedIds, string key)
        {
            var candidateKey = key;
            uint candidateId;
            var attempt = 0;

            while (true)
            {
                candidateId = WwiseHash.Compute(candidateKey);

                // If it's the max uint32 value, minus as the max value displays as -1 in wwiser and I don't trust that but max - 1 displays fine
                if (candidateId == uint.MaxValue)
                    candidateId = candidateId - 1;

                var idIsUnique = usedIds.Add(candidateId);
                if (idIsUnique)
                    return new Result(candidateId, candidateKey, key, attempt);

                attempt++;
                candidateKey = $"{key}__id_collision_{attempt}";
            }
        }

        public static Result GenerateWemId(HashSet<uint> usedHircIds, string audioProjectName, string wavFilename)
        {
            var key = $"{wavFilename}";
            var idGenerationResult = GenerateUniqueId(usedHircIds, key);
            return idGenerationResult;
        }

        public static Result GenerateSoundHircId(HashSet<uint> usedHircIds, string audioProjectName, string wavFilename)
        {
            var hirc = "sound_hirc";
            var key = $"{hirc}_{wavFilename}";
            var idGenerationResult = GenerateUniqueId(usedHircIds, key);
            return idGenerationResult;
        }

        public static Result GenerateActionHircId(HashSet<uint> usedHircIds, string audioProjectName, string actionEventName)
        {
            var hirc = "action_hirc";
            var key = $"{hirc}_{actionEventName}";
            var idGenerationResult = GenerateUniqueId(usedHircIds, key);
            return idGenerationResult;
        }

        public static Result GenerateRanSeqCntrActionEventHircId(HashSet<uint> usedHircIds, string audioProjectName, string actionEventName)
        {
            var hirc = "ran_seq_cntr_action_event_hirc";
            var key = $"{hirc}_{actionEventName}";
            var idGenerationResult = GenerateUniqueId(usedHircIds, key);
            return idGenerationResult;
        }

        public static Result GenerateRanSeqCntrDialogueEventHircId(HashSet<uint> usedHircIds, string audioProjectName, string dialogueEventName, StatePath statePath)
        {
            var hirc = "ran_seq_cntr_dialogue_event_hirc";
            var joinedStatePath = string.Empty;
            foreach (var node in statePath.Nodes)
            {
                var stateName = node.State.Name;
                if (!string.IsNullOrEmpty(joinedStatePath))
                    joinedStatePath += ".";
                joinedStatePath += stateName;
            }
            var key = $"{hirc}_{dialogueEventName}_{joinedStatePath}";
            var idGenerationResult = GenerateUniqueId(usedHircIds, key);
            return idGenerationResult;
        }
    }
}
