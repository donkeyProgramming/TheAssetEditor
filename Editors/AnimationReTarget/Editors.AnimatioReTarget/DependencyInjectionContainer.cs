using AnimationEditor.Common.BaseControl;
using Editors.AnimatioReTarget.Editor;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.BoneHandling.Presentation;
using Editors.AnimatioReTarget.Editor.Settings;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;

namespace Editors.AnimatioReTarget
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<AnimationRetargetViewModel>();
            serviceCollection.AddScoped<BoneManager>();
            RegisterWindow<BoneMappingWindow>(serviceCollection);
            serviceCollection.AddScoped<AnimationReTargetRenderingComponent>();


            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
              .Create<AnimationRetargetViewModel, EditorHostView>(EditorEnums.AnimationTransfer_Editor)
              .AddToToolbar("Animation ReTarget Tool", false)
              .Build(database);


        }
    }
}
