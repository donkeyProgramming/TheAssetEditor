using System.Windows;
using System.Windows.Controls;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Exporting.Presentation.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Presentation.DdsToPng;

namespace Editors.ImportExport.Exporting.Presentation
{
    public class ExportSettingsTemplateSelector : DataTemplateSelector
    {
        private readonly Dictionary<Type, Type> _store = [];
        public ExportSettingsTemplateSelector()
        {
            _store[typeof(DdsToPngExporterViewModel)] = typeof(DdsToPngView);
            _store[typeof(DdsToMaterialPngViewModel)] = typeof(DdsToMaterialPngView);
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null)
            {
                var found = _store.TryGetValue(item.GetType(), out var view);
                if(found)
                {
                    var factory = new FrameworkElementFactory(view);
                    var dt = new DataTemplate
                    {
                        VisualTree = factory
                    };
                    return dt;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
