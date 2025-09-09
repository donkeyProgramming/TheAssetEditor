using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class RandomSequenceContainer : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.RandomSequenceContainer;
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public List<Sound> Sounds { get; set; }

        public static RandomSequenceContainer Create(AudioSettings audioSettings, List<Sound> sounds)
        {
            return new RandomSequenceContainer
            {
                AudioSettings = audioSettings,
                Sounds = sounds
            };
        }
    }
}
