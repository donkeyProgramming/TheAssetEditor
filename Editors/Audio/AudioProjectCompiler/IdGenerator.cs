using System;
using System.Collections.Generic;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioProjectCompiler
{
    public record IdGeneratorResult(Guid Guid, uint Id);

    public class IdGenerator()
    {
        public static IdGeneratorResult GenerateAudioProjectGeneratableItemIds(HashSet<uint> usedIds)
        {
            while (true)
            {
                var guid = Guid.NewGuid();
                var id = WwiseHash.Compute(guid.ToString());
                if (id != 0 && usedIds.Add(id))
                    return new IdGeneratorResult(guid, id);
            }
        }

        public static uint GenerateSourceId(HashSet<uint> usedIds, string filePath, bool doWeCare = true)
        {
            var sourceId = WwiseHash.Compute(filePath);
            if (usedIds.Contains(sourceId) && doWeCare == true)
                throw new InvalidOperationException($"SourceId {sourceId} is already used. Change the name of {filePath}.");
            else
                return sourceId;
        }

        public static uint GenerateActionEventId(HashSet<uint> usedIds, string actionEventName)
        {
            var id = WwiseHash.Compute(actionEventName);
            if (usedIds.Contains(id))
                throw new InvalidOperationException($"Action name {actionEventName} is already used. Change the name of the Action Event.");
            else
                return id;
        }

        public static uint GenerateActionId(HashSet<uint> usedIds, string actionName, string actionEventName)
        {
            var id = WwiseHash.Compute(actionName);
            if (usedIds.Contains(id))
                throw new InvalidOperationException($"Action name {actionName} is already used. Change the name of the Action Event {actionEventName}.");
            else
                return id;
        }
    }
}
