using AnimationEditor.Common.BaseControl;
using Editor.VisualSkeletonEditor.SkeletonEditor;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editor.VisualSkeletonEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SkeletonEditorViewModel>();
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            var editorCfg = EditorInfoBuilder
                .Create<SkeletonEditorViewModel, EditorHostView>(EditorEnums.VisualSkeletonEditor)
                .AddExtention(".anim", 0)
                .ValidForFoldersContaining("animation//skeletons")
                .ValidForFoldersContaining("tech")
                .AddToToolbar("Skeleton tool")
                .Build();

            //editorDatabase.Register(editorCfg);

            editorDatabase.Register(EditorInfo.Create<SkeletonEditorViewModel, EditorHostView>(EditorEnums.VisualSkeletonEditor, new ExtensionToTool([".anim"])));
        }
    }





    public class EditorInfo2
    {
        public record ExtentionInfo(string extention, int priority);

        public EditorInfo2(EditorEnums editorEnum, Type view, Type viewModel)
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

    public class EditorInfoBuilder
    {
        protected EditorInfo2 _instance;
        public static EditorInfoBuilder Create<T, U>(EditorEnums editorType)
        {
            return new EditorInfoBuilder()
            {
                _instance = new EditorInfo2(editorType, typeof(T), typeof(U))
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
            _instance.Extensions.Add(new EditorInfo2.ExtentionInfo(extention.Trim().ToLower(), priority));
            return this;
        }

        public EditorInfoBuilder ValidForFoldersContaining(string filter)
        {
            _instance.FolderRules.Add(filter.Trim().ToLower());  
            return this;
        }

        public EditorInfo2 Build() => _instance;


    }
}
