namespace SharedCore.ToolCreation
{
    public interface IToolSelectorUiProvider
    {
        public EditorEnums CreateAndShow(IEnumerable<EditorEnums> editors);
    }
}
