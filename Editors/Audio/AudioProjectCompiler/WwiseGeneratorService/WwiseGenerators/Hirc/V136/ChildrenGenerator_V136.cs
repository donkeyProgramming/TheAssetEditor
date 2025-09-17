using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class ChildrenGenerator_V136
    {

        public static Children_V136 CreateChildrenList(List<Sound> sounds)
        {
            var childIds = sounds
                .Select(sound => sound.Id)
                .ToList();

            return new Children_V136
            {
                ChildIds = childIds
            };
        }
    }
}
