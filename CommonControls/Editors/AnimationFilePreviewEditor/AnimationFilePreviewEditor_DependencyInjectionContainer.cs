using Common;
using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
            factory.RegisterTool<TextEditorViewModel<AnimFileToTextConverter>, TextEditorView>(new ExtentionToTool(".anim"));
            factory.RegisterTool<TextEditorViewModel<InvMatrixToTextConverter>, TextEditorView>(new ExtentionToTool(".bone_inv_trans_mats"));
        }
    }
}
