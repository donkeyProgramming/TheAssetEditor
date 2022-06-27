using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.AnimationFilePreviewEditor
{
    public class AnimationFilePreviewEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<AnimFileToTextConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<AnimFileToTextConverter>>();

            serviceCollection.AddTransient<InvMatrixToTextConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<InvMatrixToTextConverter>>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterFileTool<TextEditorViewModel<AnimFileToTextConverter>, TextEditorView>(new ExtentionToTool(EditorEnums.XML_Editor, new[] { ".anim" }));
            factory.RegisterFileTool<TextEditorViewModel<InvMatrixToTextConverter>, TextEditorView>(new ExtentionToTool(EditorEnums.XML_Editor, new[] { ".bone_inv_trans_mats" }));
        }
    }
}
