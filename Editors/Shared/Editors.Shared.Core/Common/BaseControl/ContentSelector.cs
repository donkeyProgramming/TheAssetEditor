using System.Windows.Controls;
using System.Windows;

namespace Editors.Shared.Core.Common.BaseControl
{
    public class HostTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not IEditorViewModelTypeProvider cast)
                return base.SelectTemplate(item, container);

            var factory = new FrameworkElementFactory(cast.EditorViewModelType);
            var dt = new DataTemplate
            {
                VisualTree = factory
            };

            return dt;
        }
    }
}
