using CommonControls.Common;
using CommonControls.Editors.AnimMeta.View;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.AnimMeta
{
    public class AnimMetaEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<EditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterFileTool<EditorViewModel, MainEditorView>(new ExtentionToTool(EditorEnums.Meta_Editor, new[] { ".anm.meta", ".meta", ".snd.meta" }));
        }
    }
}
