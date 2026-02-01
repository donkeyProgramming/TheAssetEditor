using System;
using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.AudioProject.Compiler
{
    public class IdGenerator()
    {
        public record Result(Guid Guid, uint Id, int Attempts);

        public static Result GenerateIds(HashSet<uint> usedIds)
        {
            var attempts = 0;
            while (true)
            {
                attempts++;
                var guid = Guid.NewGuid();
                var id = WwiseHash.Compute(guid.ToString());
                if (id != 0 && usedIds.Add(id))
                    return new Result(guid, id, attempts);
            }
        }

        public static uint GenerateActionEventId(HashSet<uint> usedIds, string actionEventName)
        {
            var id = WwiseHash.Compute(actionEventName);
            if (usedIds.Contains(id))
                throw new InvalidOperationException($"Action name {actionEventName} is already used. Change the name of the Action Event.");
            else
                return id;
        }

        public static HashSet<uint> GetUsedHircIds(IAudioRepository audioRepository, AudioProjectFile audioProject)
        {
            var usedHircIds = new HashSet<uint>();

            var languageId = WwiseHash.Compute(audioProject.Language);
            var languageHircIds = audioRepository.GetUsedVanillaHircIdsByLanguageId(languageId);
            usedHircIds.UnionWith(languageHircIds);

            var audioProjectGeneratableItemIds = audioProject.GetGeneratableItemIds();
            usedHircIds.UnionWith(audioProjectGeneratableItemIds);

            return usedHircIds;
        }

        public static HashSet<uint> GetUsedSourceIds(IAudioRepository audioRepository, AudioProjectFile audioProject)
        {
            var usedSourceIds = new HashSet<uint>();

            var languageId = WwiseHash.Compute(audioProject.Language);
            var languageSourceIds = audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);
            usedSourceIds.UnionWith(languageSourceIds);

            var audioProjectSourceIds = audioProject.GetAudioFileIds();
            usedSourceIds.UnionWith(audioProjectSourceIds);

            return usedSourceIds;
        }
    }
}
