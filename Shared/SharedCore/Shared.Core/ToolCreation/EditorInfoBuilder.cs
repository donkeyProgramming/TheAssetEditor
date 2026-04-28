namespace Shared.Core.ToolCreation
{
    public class EditorInfo
    {
        public record ExtentionInfo(string Extention, int Priority);

        public EditorInfo(EditorEnums editorEnum, Type view, Type viewModel)
        {
            EditorEnum = editorEnum;
            View = view;
            ViewModel = viewModel;
        }
        public List<ExtentionInfo> Extensions { get; set; } = new List<ExtentionInfo>();
        public List<string> FolderRules { get; set; } = new List<string>();
        public string ToolbarName { get; set; } = "";
        public bool AddToolbarButton { get; set; } = false;
        public bool IsToolbarButtonEnabled { get; set; } = false;
        public EditorEnums EditorEnum { get; }
        public Type View { get; }
        public Type ViewModel { get; }
    }

    public static class EditorPriorites
    {
        public static int Low => 0;
        public static int Default => 50;
        public static int High => 100;
    }

    public class EditorInfoBuilder
    {
        protected EditorInfo _instance;
        public static EditorInfoBuilder Create<TViewModel, TView>(EditorEnums editorType) where TViewModel : IEditorInterface
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
