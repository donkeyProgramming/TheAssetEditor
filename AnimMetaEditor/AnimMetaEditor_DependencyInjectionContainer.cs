using AnimMetaEditor.ViewModels;
using AnimMetaEditor.Views;
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
            serviceCollection.AddTransient<MainViewModel>();
            //serviceCollection.AddSingleton<SkeletonAnimationLookUpHelper>();
            ////_skeletonAnimationLookUpHelper = new (_packFileService);
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<MainViewModel, MetaDataMainView>(".anm.meta");
        }
    }
}
