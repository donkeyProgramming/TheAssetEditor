using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class RandomSequenceContainer : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.RandomSequenceContainer;
        public uint OverrideBusId { get; set; }
        public uint DirectParentId { get; set; }
        public RandomSequenceContainerSettings Settings { get; set; }
        public List<Sound> Sounds { get; set; }
        public string Language { get; set; }
    }
}
