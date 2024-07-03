using Editors.ImportExport.Exporting;
using Editors.ImportExport.Exporting.Exporters;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace Editors.ImportExport
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            services.AddTransient<ExportView>();
            services.AddTransient<ExporterViewModel>();

            services.AddTransient<IExporter, DdsToMaterialPngExporter>();
            services.AddTransient<DdsToMaterialPngViewModel>();

            services.AddTransient<DisplayExportFileToolCommand>();
        }
    }
}
