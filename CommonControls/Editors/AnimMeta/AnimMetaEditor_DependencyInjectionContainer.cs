using Common;
using CommonControls.Common;
using CommonControls.Editors.AnimMeta.View;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
            factory.RegisterTool<EditorViewModel, MainEditorView>(new ExtentionToTool(".anm.meta", ".meta"));
        }
    }
}
