using Editors.ImportExport.Exporting;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Exporting.Presentation;
using Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Presentation.DdsToNormalPng;
using Editors.ImportExport.Exporting.Presentation.DdsToPng;
using Editors.ImportExport.Exporting.Presentation.RmvToGltf;
using Editors.ImportExport.Importing;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.DevConfig;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.External;

namespace Editors.ImportExport
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public override void Register(IServiceCollection services)
        {
            // Exporter ViewModels
            RegisterWindow<ExportWindow>(services);
            services.AddTransient<ExporterCoreViewModel>();
            services.AddTransient<IExporterViewModel, DdsToPngExporterViewModel>();
            services.AddTransient<IExporterViewModel, DdsToMaterialPngViewModel>();
            services.AddTransient<IExporterViewModel, DdsToNormalPngViewModel>();
            services.AddTransient<IExporterViewModel, RmvToGltfExporterViewModel>();

            // Exporters
            services.AddTransient<IDdsToMaterialPngExporter, DdsToMaterialPngExporter>();
            services.AddTransient<DdsToPngExporter>();
            services.AddTransient<IDdsToNormalPngExporter, DdsToNormalPngExporter>();
            services.AddTransient<RmvToGltfExporter>();

            // Importers
            services.AddTransient<GltfImporter>();

            // Image Save Helper
            services.AddScoped<IImageSaveHandler, SystemImageSaveHandler>();

            // Helpers to ensure we can hook up to the UI
            services.AddTransient<IExportFileContextMenuHelper, ExportFileContextMenuHelper>();
            services.AddTransient<DisplayExportFileToolCommand>();

            services.AddTransient<IImportFileContextMenuHelper, ImportFileContextMenuHelper>();
            services.AddTransient<DisplayImportFileToolCommand>();

            services.AddTransient<GltfMeshBuilder>();
            services.AddTransient<IGltfTextureHandler, GltfTextureHandler>();
            services.AddTransient<IGltfSceneSaver, GltfSceneSaver>();
            services.AddTransient<IGltfSceneLoader, GltfSceneLoader>();
            services.AddTransient<GltfSkeletonBuilder>();
            services.AddTransient<GltfAnimationBuilder>();
            
            RegisterAllAsInterface<IDeveloperConfiguration>(services, ServiceLifetime.Transient);
        }
    }
}
