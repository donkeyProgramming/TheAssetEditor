using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.Storage;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IIdGeneratorService
    {
        IdGeneratorResult GenerateUniqueId(uint languageId, Dictionary<string, HashSet<uint>> usedIdsLookupByBnkFilePath, string bnkFilePath, string key, bool isActionEvent);
        IdGeneratorResult GenerateWemId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerSourceIds, string wavFilename);
        IdGeneratorResult GenerateSoundHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string wavFilename);
        IdGeneratorResult GenerateActionEventHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string actionEventName);
        IdGeneratorResult GenerateActionHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string actionName, int actionIndex);
        IdGeneratorResult GenerateRanSeqCntrActionEventHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string actionEventName);
        IdGeneratorResult GenerateRanSeqCntrDialogueEventHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string dialogueEventName, StatePath statePath);
    }

    public record IdGeneratorResult(uint Id, string FinalKey, string OriginalKey, int CollisionsCount, bool CollisionOverridden);

    public class IdGeneratorService (IAudioRepository audioRepository) : IIdGeneratorService
    {
        private readonly IAudioRepository _audioRepository = audioRepository;

        public IdGeneratorResult GenerateUniqueId(
            uint languageId,
            Dictionary<string, HashSet<uint>> usedIdsLookupByBnkFilePath,
            string bnkFilePath,
            string originalKey,
            bool isActionEvent = false)
        {
            var usedIds = usedIdsLookupByBnkFilePath.Values.SelectMany(ids => ids).ToHashSet();
            usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var compilerBnkIds);
            var candidateKey = originalKey;
            uint candidateId;
            var attempt = 1;

            while (true)
            {
                candidateId = WwiseHash.Compute(candidateKey);

                // The max value displays as -1 in wwiser and I don't trust that but max - 1 displays the ID fine
                if (candidateId == uint.MaxValue)
                    candidateId--;

                var idIsUnique = !usedIds.Contains(candidateId);

                // If the ID is in a bnk with the same name as that which we're compiling (i.e. we're overriding a previously compiled bnk) then override the collision
                var overrideCollision = false;
                if (compilerBnkIds != null)
                    overrideCollision = compilerBnkIds.Contains(candidateId);

                if (idIsUnique || overrideCollision)
                    return new IdGeneratorResult(candidateId, candidateKey, originalKey, attempt, overrideCollision);

                // Because an Action Event's ID is the hash of what the user names it we should tell the user to rename it rather than modifying the suffix
                if (isActionEvent)
                    throw new NotSupportedException($"Action Event {originalKey} (ID: {candidateId}) in {_audioRepository.GetNameFromId(languageId)} is already in use. Rename Action Event.");

                candidateKey = $"{originalKey}__id_collision_{attempt}";
                attempt++;
            }
        }

        public IdGeneratorResult GenerateWemId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerSourceIds, string wavFilename)
        {
            var usedIdsLookupByBnkFilePath = _audioRepository.GetUsedSourceIdsLookupByBnkFilePathByLanguageId(languageId);
            if (usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var bnkUsedIds))
                bnkUsedIds.UnionWith(usedCompilerSourceIds);

            var key = $"{wavFilename}";
            var idGenerationResult = GenerateUniqueId(languageId, usedIdsLookupByBnkFilePath, bnkFilePath, key);
            return idGenerationResult;
        }

        public IdGeneratorResult GenerateSoundHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string wavFilename)
        {
            var usedIdsLookupByBnkFilePath = _audioRepository.GetUsedHircIdsLookupByBnkFilePathByLanguageId(languageId);
            if (usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var bnkUsedIds))
                bnkUsedIds.UnionWith(usedCompilerHircIds);

            var key = $"sound_hirc_{wavFilename}";
            var idGenerationResult = GenerateUniqueId(languageId, usedIdsLookupByBnkFilePath, bnkFilePath, key);
            return idGenerationResult;
        }

        public IdGeneratorResult GenerateActionEventHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string actionEventName)
        {
            var usedIdsLookupByBnkFilePath = _audioRepository.GetUsedHircIdsLookupByBnkFilePathByLanguageId(languageId);
            if (usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var bnkUsedIds))
                bnkUsedIds.UnionWith(usedCompilerHircIds);

            var idGenerationResult = GenerateUniqueId(languageId, usedIdsLookupByBnkFilePath, bnkFilePath, actionEventName);
            return idGenerationResult;
        }

        public IdGeneratorResult GenerateActionHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string actionName, int actionIndex)
        {
            var usedIdsLookupByBnkFilePath = _audioRepository.GetUsedHircIdsLookupByBnkFilePathByLanguageId(languageId);
            if (usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var bnkUsedIds))
                bnkUsedIds.UnionWith(usedCompilerHircIds);

            var key = $"action_hirc_{actionName}_{actionIndex}";
            var idGenerationResult = GenerateUniqueId(languageId, usedIdsLookupByBnkFilePath, bnkFilePath, key);
            return idGenerationResult;
        }

        public IdGeneratorResult GenerateRanSeqCntrActionEventHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string actionEventName)
        {
            var usedIdsLookupByBnkFilePath = _audioRepository.GetUsedHircIdsLookupByBnkFilePathByLanguageId(languageId);
            if (usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var bnkUsedIds))
                bnkUsedIds.UnionWith(usedCompilerHircIds);

            var key = $"ran_seq_cntr_action_event_hirc_{actionEventName}";
            var idGenerationResult = GenerateUniqueId(languageId, usedIdsLookupByBnkFilePath, bnkFilePath, key);
            return idGenerationResult;
        }

        public IdGeneratorResult GenerateRanSeqCntrDialogueEventHircId(uint languageId, string bnkFilePath, HashSet<uint> usedCompilerHircIds, string dialogueEventName, StatePath statePath)
        {
            var usedIdsLookupByBnkFilePath = _audioRepository.GetUsedHircIdsLookupByBnkFilePathByLanguageId(languageId);
            if (usedIdsLookupByBnkFilePath.TryGetValue(bnkFilePath, out var bnkUsedIds))
                bnkUsedIds.UnionWith(usedCompilerHircIds);

            var joinedStatePath = string.Empty;
            foreach (var node in statePath.Nodes)
            {
                var stateName = node.State.Name;
                if (!string.IsNullOrEmpty(joinedStatePath))
                    joinedStatePath += ".";
                joinedStatePath += stateName;
            }
            var key = $"ran_seq_cntr_dialogue_event_hirc_{dialogueEventName}_{joinedStatePath}";
            var idGenerationResult = GenerateUniqueId(languageId, usedIdsLookupByBnkFilePath, bnkFilePath, key);
            return idGenerationResult;
        }
    }
}
