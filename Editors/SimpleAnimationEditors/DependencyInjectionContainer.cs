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

        public override void RegisterTools(IEditorDatabase database)
        {
            EditorInfoBuilder
                .Create<AnimPackViewModel, AnimationPackView>(EditorEnums.AnimationPack_Editor)
                .AddExtention(".animpack", EditorPriorites.High)
                .Build(database);

            EditorInfoBuilder
                .Create<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(EditorEnums.XML_CampaginBin_Edtior)
                .AddExtention(".bin", EditorPriorites.High)
                .ValidForFoldersContaining(@"animations\campaign\database")
                .Build(database);
        
            EditorInfoBuilder
                .Create<TextEditorViewModel<AnimFileToTextConverter>, TextEditorView>(EditorEnums.XML_Anim_Editor)
                .AddExtention(".anim", EditorPriorites.Default)
                .Build(database);

            EditorInfoBuilder
                .Create<TextEditorViewModel<InvMatrixToTextConverter>, TextEditorView>(EditorEnums.XML_InvBoneEditor)
                .AddExtention(".bone_inv_trans_mats", EditorPriorites.Default)
                .Build(database);


            EditorInfoBuilder
                .Create<AnimationBatchExportViewModel, AnimationBatchExportView>(EditorEnums.AnimationBatchExporter_Editor)
                .AddToToolbar("Animation Batch Exporter")
                .Build(database);
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
