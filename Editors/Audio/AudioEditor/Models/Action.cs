using Shared.GameFormats.Wwise.Enums;

namespace Editors.Audio.AudioEditor.Models
{
    public class Action : AudioProjectHircItem
    {
        public override AkBkHircType HircType { get; set; } = AkBkHircType.Action;
        public AkActionType ActionType { get; set; } = AkActionType.Play;
        public uint IdExt { get; set; }
    }
}
