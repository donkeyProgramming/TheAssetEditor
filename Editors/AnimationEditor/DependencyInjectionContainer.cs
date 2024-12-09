using AnimationEditor.AnimationTransferTool;
using AnimationEditor.CampaignAnimationCreator;
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
            serviceCollection.AddScoped<EditorHost<CampaignAnimationCreatorViewModel>>();
            serviceCollection.AddScoped<CampaignAnimationCreatorViewModel>();

            serviceCollection.AddScoped<EditorHost<AnimationTransferToolViewModel>>();
            serviceCollection.AddScoped<AnimationTransferToolViewModel>();

            serviceCollection.AddScoped<EditorHost<MountAnimationCreatorViewModel>>();
            serviceCollection.AddScoped<MountAnimationCreatorViewModel>();

            serviceCollection.AddScoped<EditorHost<AnimationKeyframeEditorViewModel>>();
            serviceCollection.AddScoped<AnimationKeyframeEditorViewModel>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
                .Create<EditorHost<MountAnimationCreatorViewModel>, EditorHostView>(EditorEnums.MountTool_Editor)
                .AddToToolbar("Mount Tool", false)
                .Build(database);
      
            EditorInfoBuilder
              .Create<EditorHost<AnimationTransferToolViewModel>, EditorHostView> (EditorEnums.AnimationTransfer_Editor)
              .AddToToolbar("Animation Transfer Tool", false)
              .Build(database);
    
            EditorInfoBuilder
              .Create<EditorHost<CampaignAnimationCreatorViewModel>, EditorHostView>(EditorEnums.CampaginAnimation_Editor)
              .AddToToolbar("Campagin Aanimation Tool", false)
              .Build(database);
        
            EditorInfoBuilder
              .Create<EditorHost<AnimationKeyframeEditorViewModel>, EditorHostView>(EditorEnums.AnimationKeyFrame_Editor)
              .AddToToolbar("KeyFrame Tool", false)
              .Build(database);
        }
    }
}
