using AnimationEditor.Common.BaseControl;
using AnimationEditor.PropCreator;
using AnimationEditor.PropCreator.ViewModels;
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
            serviceCollection.AddTransient<PropCreatorViewModel>();
            //serviceCollection.AddTransient<MainPropCreatorView>();
            serviceCollection.AddTransient<BaseAnimationView>();


            //serviceCollection.AddTransient<MainEditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            //factory.RegisterTool<MainEditorViewModel, MainEditorView>(".anm.meta");
            factory.RegisterTool<PropCreatorViewModel, BaseAnimationView>();
        }
    }
}
