using AnimationEditor.Common.BaseControl;
using Editor.CampaignAnimationCreator.CampaignAnimationCreator;
using Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands;
using Editors.Shared.Core.Common.BaseControl;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Core.ToolCreation;

namespace Editor.CampaignAnimationCreator
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<CampaignAnimationCreatorViewModel>();
            serviceCollection.AddScoped<EditorView>();
            serviceCollection.AddTransient<ConvertCampaignAnimationCommand>();
            serviceCollection.AddTransient<SaveCampaignAnimationCommand>();

            RegisterAllAsInterface<IDeveloperConfiguration>(serviceCollection, ServiceLifetime.Transient);
        }

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
                .Create<CampaignAnimationCreatorViewModel, EditorHostView>(EditorEnums.CampaginAnimation_Editor)
                .AddToToolbar("Campaign Animation Tool", true)
                .Build(database);
        }
    }
}
