using System.Windows;
using System.Windows.Controls;
using Editors.ImportExport.Exporting.Exporters;

namespace Editors.ImportExport.Exporting.Presentation
{
    public class ExportSettingsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not IExporterViewModel cast)
                return base.SelectTemplate(item, container);

            var factory = new FrameworkElementFactory(cast.ViewType);
            var dt = new DataTemplate
            {
                VisualTree = factory
            };

            return dt;
        }
    }
}
