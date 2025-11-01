using System;
using System.Collections.Generic;
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
    }
}
