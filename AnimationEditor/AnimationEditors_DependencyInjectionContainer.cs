using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.Common.ReferenceModel;
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
            serviceCollection.AddScoped<AssetViewModelBuilder>();
            serviceCollection.AddTransient<AssetViewModel>();
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<ReferenceModelSelectionViewModelBuilder>();
            
            serviceCollection.AddScoped<SuperView.Editor>();
            serviceCollection.AddScoped<SkeletonEditor.Editor>();
            serviceCollection.AddScoped<MountAnimationCreator.Editor>();
            serviceCollection.AddScoped<CampaignAnimationCreator.Editor>();
            serviceCollection.AddScoped<AnimationTransferTool.Editor>();

            serviceCollection.AddScoped<MountAnimationCreatorViewModel>();
            serviceCollection.AddScoped<CampaignAnimationCreatorViewModel>();
            serviceCollection.AddScoped<AnimationTransferToolViewModel>();
            serviceCollection.AddScoped<SuperViewViewModel>();
            serviceCollection.AddScoped<SkeletonEditorViewModel>();
           // serviceCollection.AddScoped<AnimationBuilderViewModel>();
            serviceCollection.AddScoped<BaseAnimationView>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<MountAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<CampaignAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<AnimationTransferToolViewModel, BaseAnimationView>();
            factory.RegisterTool<SuperViewViewModel, BaseAnimationView>();
            factory.RegisterTool<SkeletonEditorViewModel, BaseAnimationView>();
           // factory.RegisterTool<AnimationBuilderViewModel, BaseAnimationView>();
        }
    }
}
