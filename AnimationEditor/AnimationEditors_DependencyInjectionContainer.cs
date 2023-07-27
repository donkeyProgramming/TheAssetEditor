using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.BaseControl;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.MountAnimationCreator;
using AnimationEditor.SkeletonEditor;
using AnimationEditor.SuperView;
using CommonControls;
using CommonControls.Services.ToolCreation;
using Microsoft.Extensions.DependencyInjection;

namespace AnimationEditor
{
    public class AnimationEditors_DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<SceneObjectBuilder>();
            serviceCollection.AddTransient<SceneObject>();
            serviceCollection.AddScoped<AnimationPlayerViewModel>();
            serviceCollection.AddScoped<SceneObjectViewModelBuilder>();

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
            serviceCollection.AddScoped<BaseAnimationView>();
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<MountAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<CampaignAnimationCreatorViewModel, BaseAnimationView>();
            factory.RegisterTool<AnimationTransferToolViewModel, BaseAnimationView>();
            factory.RegisterTool<SuperViewViewModel, BaseAnimationView>();
            factory.RegisterTool<SkeletonEditorViewModel, BaseAnimationView>();
        }
    }
}
