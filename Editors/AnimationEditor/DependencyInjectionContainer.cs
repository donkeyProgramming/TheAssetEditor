using AnimationEditor.Common.BaseControl;
using AnimationEditor.MountAnimationCreator;
using Editors.AnimationVisualEditors.AnimationKeyframeEditor;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;

namespace Editors.AnimationVisualEditors
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {

            //serviceCollection.AddScoped<AnimationRetargetEditor>();
            //serviceCollection.AddScoped<BoneManager>();
            //serviceCollection.AddScoped<SaveManager>();
            //serviceCollection.AddScoped<SaveSettings>();
            //serviceCollection.AddScoped<AnimationReTargetRenderingComponent>();

            //RegisterWindow<BoneMappingWindow>(serviceCollection);
            //RegisterWindow<SaveWindow>(serviceCollection);

            serviceCollection.AddScoped<AnimationToolInput, AnimationToolInput>();

            //serviceCollection.AddScoped<EditorHost<CampaignAnimationCreatorViewModel>>();
            //serviceCollection.AddScoped<CampaignAnimationCreatorViewModel>();
            serviceCollection.AddScoped<MountAnimationCreatorViewModel>();

            // Use the new EditorHostBase-based view model directly for the Keyframe editor
            serviceCollection.AddScoped<AnimationKeyframeEditorViewModel>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
                .Create<MountAnimationCreatorViewModel, EditorHostView>(EditorEnums.MountTool_Editor)
                .AddToToolbar("Mount Tool", true)
                .Build(database);

            // Register the Keyframe editor using its EditorHostBase-based type
            EditorInfoBuilder
              .Create<AnimationKeyframeEditorViewModel, EditorHostView>(EditorEnums.AnimationKeyFrame_Editor)
              .AddToToolbar("KeyFrame Tool", true)
              .Build(database);
        }
    }
}
