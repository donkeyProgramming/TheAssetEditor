using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.MountAnimationCreator;
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
            serviceCollection.AddTransient<MountAnimationCreatorViewModel>();
            serviceCollection.AddTransient<CampaignAnimationCreatorViewModel>();

            serviceCollection.AddTransient<BaseAnimationView>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<PropCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<MountAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<CampaignAnimationCreatorViewModel, BaseAnimationView>();
        }
    }
}
