using AnimMetaEditor.ViewModels;
using AnimMetaEditor.ViewModels.Editor;
using AnimMetaEditor.Views;
using AnimMetaEditor.Views.Editor;
using Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimMetaEditor
{
    public class AnimMetaEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MetaDataMainView>();
            serviceCollection.AddTransient<MainDecoderViewModel>();
            
            serviceCollection.AddTransient<MainEditorView>();
            serviceCollection.AddTransient<MainEditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<MainEditorViewModel, MainEditorView>(".anm.meta", ".meta");
            factory.RegisterTool<MainDecoderViewModel, MetaDataMainView>();
        }
    }
}
