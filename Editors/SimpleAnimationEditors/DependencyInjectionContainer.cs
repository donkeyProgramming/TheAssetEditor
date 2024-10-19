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

        public override void RegisterTools(IEditorDatabase factory)
        {
            factory.Register(EditorInfo.Create<AnimPackViewModel, AnimationPackView>(EditorEnums.AnimationPack_Editor, new ExtensionToTool([".animpack"])));
            factory.Register(EditorInfo.Create<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(EditorEnums.XML_CampaginBin_Edtior, new PathToTool(".bin", @"animations\campaign\database")));
            factory.Register(EditorInfo.Create<TextEditorViewModel<AnimFileToTextConverter>, TextEditorView>(EditorEnums.XML_Anim_Editor, new ExtensionToTool([".anim"])));
            factory.Register(EditorInfo.Create<TextEditorViewModel<InvMatrixToTextConverter>, TextEditorView>(EditorEnums.XML_InvBoneEditor, new ExtensionToTool([".bone_inv_trans_mats"])));
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
