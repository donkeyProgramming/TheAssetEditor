using CommonControls.Editors.AnimationBatchExporter;
using CommonControls.Editors.AnimationFilePreviewEditor;
using CommonControls.Editors.AnimationPack;
using CommonControls.Editors.CampaignAnimBin;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.Ui.Editors.TextEditor;

namespace Editors.AnimationTextEditors
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            RegisterAnimPack(services);
            RegisterCampaignAnimBin(services);
            RegisterAnimFileViewer(services);
            RegisterBatchConverter(services);
        }

        public override void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AnimPackViewModel, AnimationPackView>(new ExtensionToTool(EditorEnums.AnimationPack_Editor, new[] { ".animpack" }));
            factory.RegisterTool<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(new PathToTool(EditorEnums.XML_Editor, ".bin", @"animations\campaign\database"));
            factory.RegisterTool<TextEditorViewModel<AnimFileToTextConverter>, TextEditorView>(new ExtensionToTool(EditorEnums.XML_Editor, new[] { ".anim" }));
            factory.RegisterTool<TextEditorViewModel<InvMatrixToTextConverter>, TextEditorView>(new ExtensionToTool(EditorEnums.XML_Editor, new[] { ".bone_inv_trans_mats" }));
        }

        private static void RegisterCampaignAnimBin(IServiceCollection services)
        {
            services.AddTransient<CampaignAnimBinToXmlConverter>();
            services.AddTransient<TextEditorViewModel<CampaignAnimBinToXmlConverter>>();
        }

        private static void RegisterAnimFileViewer(IServiceCollection services)
        {
            services.AddTransient<AnimFileToTextConverter>();
            services.AddTransient<TextEditorViewModel<AnimFileToTextConverter>>();

            services.AddTransient<InvMatrixToTextConverter>();
            services.AddTransient<TextEditorViewModel<InvMatrixToTextConverter>>();
        }

        private static void RegisterBatchConverter(IServiceCollection services)
        {
            services.AddTransient<OpenAnimationBatchConverterCommand>();
            services.AddTransient<AnimationBatchExportViewModel>();
            services.AddTransient<AnimationBatchExportView>();
        }

        private static void RegisterAnimPack(IServiceCollection services)
        {
            services.AddTransient<AnimationPackView>();
            services.AddTransient<AnimPackViewModel>();
        }
    }
}
