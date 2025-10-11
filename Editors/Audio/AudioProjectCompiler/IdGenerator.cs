using System;
using System.Collections.Generic;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioProjectCompiler
{
    public record IdGeneratorResult(Guid Guid, uint Id, int Attempts);

    public class IdGenerator()
    {
        public static IdGeneratorResult GenerateIds(HashSet<uint> usedIds)
        {
            var attempts = 0;
            while (true)
            {
                attempts++;
                var guid = Guid.NewGuid();
                var id = WwiseHash.Compute(guid.ToString());
                if (id != 0 && usedIds.Add(id))
                    return new IdGeneratorResult(guid, id, attempts);
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
    }
}
