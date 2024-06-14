using Shared.GameFormats.WWise;

namespace Editors.Audio.Presentation.AudioExplorer
{
    public class SelectedHircItem
    {
        public string DisplayName { get; set; }
        public uint Id { get; set; }
        public string PackFile { get; set; }
        public uint IndexInFile { get; set; }
        public HircItem HircItem { get; set; }
    }
}
