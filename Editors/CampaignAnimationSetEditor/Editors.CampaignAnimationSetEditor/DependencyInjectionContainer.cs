using Editors.CampaignAnimationSetEditor.Services;
using Editors.CampaignAnimationSetEditor.ViewModels;
using Editors.CampaignAnimationSetEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Editors.CampaignAnimationSetEditor
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection serviceCollection)
        {
            // Views
            serviceCollection.AddTransient<CampaignAnimationSetEditorView>();

            // ViewModels
            serviceCollection.AddScoped<CampaignAnimationSetEditorViewModel>();
            serviceCollection.AddScoped<IEditorInterface, CampaignAnimationSetEditorViewModel>();

            // Services
            serviceCollection.AddTransient<FreezeRootBoneCommand>();
            serviceCollection.AddTransient<CampaignAnimationSetGeneratorService>();
        }

        public override void RegisterTools(IEditorDatabase editorDatabase)
        {
            EditorInfoBuilder
                .Create<CampaignAnimationSetEditorViewModel, CampaignAnimationSetEditorView>(EditorEnums.XML_CampaginBin_Edtior)
                .AddExtention(".bin", EditorPriorites.High)
                .ValidForFoldersContaining(@"animations\campaign\database")
                .AddToToolbar("Campaign Animation Set Editor", true)
                .Build(editorDatabase);
        }
    }
}
