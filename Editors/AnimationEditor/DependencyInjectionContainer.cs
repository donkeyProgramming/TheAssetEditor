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
            serviceCollection.AddScoped<MountAnimationCreatorViewModel>();

            serviceCollection.AddScoped<AnimationKeyframeEditorViewModel>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
                .Create<MountAnimationCreatorViewModel, EditorHostView>(EditorEnums.MountTool_Editor)
                .AddToToolbar("Mount Tool", true)
                .Build(database);
        
            EditorInfoBuilder
              .Create<AnimationKeyframeEditorViewModel, EditorHostView>(EditorEnums.AnimationKeyFrame_Editor)
              .AddToToolbar("KeyFrame Tool", true)
              .Build(database);
        }
    }
}
