using System.Collections.ObjectModel;
using Editors.ImportExport.Exporting.Exporters;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Presentation
{
    internal class ExporterViewModel : NotifyPropertyChangedImpl
    {
        private readonly List<IExporterViewModel> _exportViewModels = new();
        public NotifyAttr<IExporterViewModel> SelectedExporterViewModel { get; set; } = new NotifyAttr<IExporterViewModel>();
        public ObservableCollection<string> PossibleExporters { get; set; } = new ObservableCollection<string>();

        public NotifyAttr<string> SelectedExporter { get; set; } = new NotifyAttr<string>();

        public NotifyAttr<string> SystemPath { get; set; } = new NotifyAttr<string>("C:\\myfile.dds");

        public ExporterViewModel(IEnumerable<IExporterViewModel> exporterViewModels)
        {
            foreach (var model in exporterViewModels)
                _exportViewModels.Add(model);

            SelectedExporter.PropertyChanged += OnExporterTypeSelected;
        }

        private void OnExporterTypeSelected(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SelectedExporterViewModel.Value = _exportViewModels.First(x => x.Exporter.Name == SelectedExporter.Value);


            //throw new NotImplementedException();
        }

        public void Initialize(PackFile packFile)
        {
            // Determine possible exporters
            PossibleExporters.Clear();

            foreach (var model in _exportViewModels)
                PossibleExporters.Add(model.Exporter.Name);

            SelectedExporter.Value = PossibleExporters.First(); ;
        }

        public void Export()
        {
            SelectedExporterViewModel.Value.Execute(SystemPath.Value, true);
        }
    }
}
