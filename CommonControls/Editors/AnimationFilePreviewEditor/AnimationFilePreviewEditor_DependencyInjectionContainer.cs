using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.Services.ToolCreation;
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
            factory.RegisterTool<TextEditorViewModel<AnimFileToTextConverter>, TextEditorView>(new ExtensionToTool(EditorEnums.XML_Editor, new[] { ".anim" }));
            factory.RegisterTool<TextEditorViewModel<InvMatrixToTextConverter>, TextEditorView>(new ExtensionToTool(EditorEnums.XML_Editor, new[] { ".bone_inv_trans_mats" }));
        }
    }
}
