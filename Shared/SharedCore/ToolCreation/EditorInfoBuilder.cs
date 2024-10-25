namespace Shared.Core.ToolCreation
{
    public class EditorInfoBuilder
    {
        protected EditorInfo _instance;
        public static EditorInfoBuilder Create<TViewModel, TView>(EditorEnums editorType) where TViewModel : EditorInterfaces
        {
            return new EditorInfoBuilder()
            {
                _instance = new EditorInfo(editorType, typeof(TView), typeof(TViewModel))
            };
        }

        public EditorInfoBuilder AddToToolbar(string toolbarLabel, bool enabled = true)
        {
            _instance.ToolbarName = toolbarLabel;
            _instance.AddToolbarButton = true;
            _instance.IsToolbarButtonEnabled = enabled;
            return this;
        }

        public EditorInfoBuilder AddExtention(string extention, int priority)
        {
            // Ensure type is IFileEditor

            _instance.Extensions.Add(new EditorInfo.ExtentionInfo(extention.Trim().ToLower(), priority));
            return this;
        }

        public EditorInfoBuilder ValidForFoldersContaining(string filter)
        {
            _instance.FolderRules.Add(filter.Trim().ToLower());
            return this;
        }

        public void Build(IEditorDatabase editorDatabase)
        {
            _instance.Extensions = _instance.Extensions.OrderBy(x => x.Priority).ToList();
            editorDatabase.Register(_instance);
        }
    }
}
