using Common;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimationPack_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<AnimationPackView>();
            serviceCollection.AddTransient<AnimPackViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AnimPackViewModel, AnimationPackView>(new ExtentionToTool(".animpack"));
        }
    }

    public static class AnimationPackEditor_Debug
    {
        public static void Load(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"animations\animation_tables\animation_tables.animpack");
            creator.OpenFile(packFile);
        }
    }
}
