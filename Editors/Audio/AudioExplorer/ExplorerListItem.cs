using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioExplorer
{
    public class ExplorerListItem
    {
        public string DisplayName { get; set; }
        public uint Id { get; set; }
        public string PackFile { get; set; }
        public uint IndexInFile { get; set; }
        public HircItem HircItem { get; set; }
    }
}
