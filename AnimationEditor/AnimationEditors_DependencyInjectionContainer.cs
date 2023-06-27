using AnimationEditor.AnimationBuilder;
using AnimationEditor.AnimationKeyframeEditor;
using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.SkeletonEditor;
using AnimationEditor.SuperView;
using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AnimationEditor
{
    public class AnimationEditors_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<AnimationKeyframeEditorViewModel>();
            serviceCollection.AddTransient<MountAnimationCreatorViewModel>();
            serviceCollection.AddTransient<CampaignAnimationCreatorViewModel>();
            serviceCollection.AddTransient<AnimationTransferToolViewModel>();
            serviceCollection.AddTransient<SuperViewViewModel>();
            serviceCollection.AddTransient<SkeletonEditorViewModel>();
            serviceCollection.AddTransient<AnimationBuilderViewModel>();
            serviceCollection.AddTransient<BaseAnimationView>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AnimationKeyframeEditorViewModel, BaseAnimationView>();
            factory.RegisterTool<MountAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<CampaignAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<AnimationTransferToolViewModel, BaseAnimationView>();
            factory.RegisterTool<SuperViewViewModel, BaseAnimationView>();
            factory.RegisterTool<SkeletonEditorViewModel, BaseAnimationView>();
            factory.RegisterTool<AnimationBuilderViewModel, BaseAnimationView>();
        }
    }
}
