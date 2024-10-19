namespace Shared.Core.ToolCreation
{
    class ToolInformation
    {
        public EditorEnums EditorType { get; set; }
        public Type Type { get; set; }
    }

    public class PackFileToToolSelectorResult
    {
        public bool CanOpen { get; set; } = false;
    }


}
