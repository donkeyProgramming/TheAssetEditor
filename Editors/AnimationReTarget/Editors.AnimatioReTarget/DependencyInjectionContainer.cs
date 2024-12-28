using AnimationEditor.Common.BaseControl;
using Editors.AnimatioReTarget.Editor;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.BoneHandling.Presentation;
using Editors.AnimatioReTarget.Editor.Saving;
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
            serviceCollection.AddScoped<AnimationRetargetEditor>();
            serviceCollection.AddScoped<BoneManager>();
            serviceCollection.AddScoped<SaveManager>();
            serviceCollection.AddScoped<SaveSettings>();
            serviceCollection.AddScoped<AnimationReTargetRenderingComponent>();

            RegisterWindow<BoneMappingWindow>(serviceCollection);
            RegisterWindow<SaveWindow>(serviceCollection);
          
            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
              .Create<AnimationRetargetEditor, EditorHostView>(EditorEnums.AnimationRetarget_Editor)
              .AddToToolbar("Animation ReTarget Tool", true)
              .Build(database);
        }
    }
}
