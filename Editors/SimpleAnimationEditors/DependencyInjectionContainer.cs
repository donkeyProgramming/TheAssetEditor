using CommonControls.Editors.AnimationBatchExporter;
using CommonControls.Editors.AnimationFilePreviewEditor;
using CommonControls.Editors.AnimationPack;
using CommonControls.Editors.CampaignAnimBin;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Misc;
using Shared.Core.ToolCreation;

namespace Editors.AnimationContainers
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer(bool loadResource = true)
        {
        }

        public override void Register(IServiceCollection services)
        {
            AnimationPack_DependencyInjectionContainer.Register(services);
            CampaignAnimBin_DependencyInjectionContainer.Register(services);
            AnimationFilePreviewEditor_DependencyInjectionContainer.Register(services);
            AnimationBatchExporter_DependencyInjectionContainer.Register(services);
        }

        public override void RegisterTools(IToolFactory factory)
        {
            AnimationPack_DependencyInjectionContainer.RegisterTools(factory);
            CampaignAnimBin_DependencyInjectionContainer.RegisterTools(factory);
            AnimationFilePreviewEditor_DependencyInjectionContainer.RegisterTools(factory);
            AnimationBatchExporter_DependencyInjectionContainer.RegisterTools(factory);
        }
    }
}
