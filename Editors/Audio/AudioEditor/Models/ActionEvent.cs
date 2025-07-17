using System.Collections.Generic;
using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class ActionEvent : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.Event;
        public List<Action> Actions { get; set; }
        // Actions should contain the Sound / RandomSequenceContainer rather than the ActionEvent but making multiple Actions for an ActionEvent isn't currently supported by the tool so not needed.
        public Sound Sound { get; set; }
        public RandomSequenceContainer RandomSequenceContainer { get; set; }
    }
}
