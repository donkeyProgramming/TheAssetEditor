using AnimationEditor.PropCreator.ViewModels;
using AnimationEditor.PropCreator.Views;
using Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor
{
    public class AnimationEditors_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<MainPropCreatorViewModel>();
            serviceCollection.AddTransient<MainPropCreatorView>();
            //serviceCollection.AddTransient<MainDecoderViewModel>();


            //serviceCollection.AddTransient<MainEditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            //factory.RegisterTool<MainEditorViewModel, MainEditorView>(".anm.meta");
            factory.RegisterTool<MainPropCreatorViewModel, MainPropCreatorView>();
        }
    }
}
