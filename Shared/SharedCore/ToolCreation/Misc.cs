namespace SharedCore.ToolCreation
{
    class ToolInformation
    {
        public EditorEnums EditorType { get; set; }
        public bool IsCoreTool { get; set; } = false;
        public Type Type { get; set; }
    }

    public class PackFileToToolSelectorResult
    {
        public bool CanOpen { get; set; } = false;
        public bool IsCoreTool { get; set; } = false;
    }

    public interface IPackFileToToolSelector
    {
        PackFileToToolSelectorResult CanOpen(string fullPath);
        EditorEnums EditorType { get; }
    }
}
