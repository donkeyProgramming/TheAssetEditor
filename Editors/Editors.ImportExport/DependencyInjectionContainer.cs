using Editors.ImportExport.Exporting;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Editors.ImportExport.Exporting.Presentation;
using Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Presentation.DdsToNormalPng;
using Editors.ImportExport.Exporting.Presentation.DdsToPng;
using Editors.ImportExport.Exporting.Presentation.RmvToGltf;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.PackFileBrowser;

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
            services.AddTransient<DdsToMaterialPngExporter>();
            services.AddTransient<DdsToPngExporter>();
            services.AddTransient<DdsToNormalPngExporter>();
            services.AddTransient<RmvToGltfExporter>();

            // Helpers to ensure we can hook up to the UI
            services.AddTransient<IExportFileContextMenuHelper, ExportFileContextMenuHelper>();
            services.AddTransient<DisplayExportFileToolCommand>();
        }
    }
}
