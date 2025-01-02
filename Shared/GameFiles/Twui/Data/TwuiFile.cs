namespace Shared.GameFormats.Twui.Data
{
    public class TwuiFile 
    {
        public FileMetaData FileMetaData { get; set; } = new();
        public List<Component> Components { get; set; } = [];
        public Hierarchy Hierarchy { get; set; } = new();
    }

    public class FileMetaData
    {
        public float Version { get; set; } = 0;
    }
}
