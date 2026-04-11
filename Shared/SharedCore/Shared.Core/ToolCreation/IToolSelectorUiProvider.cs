namespace Shared.Core.ToolCreation
{
    public interface IToolSelectorUiProvider
    {
        public EditorEnums CreateAndShow(IEnumerable<EditorEnums> editors);
    }
}
