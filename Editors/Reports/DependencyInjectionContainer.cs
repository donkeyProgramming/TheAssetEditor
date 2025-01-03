using Editors.Reports.Animation;
using Editors.Reports.Audio;
using Editors.Reports.DeepSearch;
using Editors.Reports.Files;
using Editors.Reports.Geometry;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace Editors.Reports
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<DeepSearchReport>();
            serviceCollection.AddTransient<DeepSearchCommand>();

            serviceCollection.AddTransient<MaterialReportCommand>();
            serviceCollection.AddTransient<MaterialReportGenerator>();

            serviceCollection.AddTransient<FileListReportCommand>();
            serviceCollection.AddTransient<FileListReportGenerator>();

            serviceCollection.AddTransient<GenerateMetaJsonDataReportCommand>();
            serviceCollection.AddTransient<AnimMetaDataJsonGenerator>();

            serviceCollection.AddTransient<GenerateMetaDataReportCommand>();
            serviceCollection.AddTransient<AnimMetaDataReportGenerator>();

            serviceCollection.AddTransient<Rmv2ReportCommand>();
            serviceCollection.AddTransient<Rmv2ReportGenerator>();

            serviceCollection.AddTransient<GenerateDialogueEventInfoPrinterReportCommand>();
            serviceCollection.AddTransient<DialogueEventInfoPrinter>();

            serviceCollection.AddTransient<GenerateDialogueEventAndEventNamePrinterReportCommand>();
            serviceCollection.AddTransient<DialogueEventAndEventNamePrinter>();

            serviceCollection.AddTransient<GenerateDatDumperReportCommand>();
            serviceCollection.AddTransient<DatDumper>();
        }
    }
}
