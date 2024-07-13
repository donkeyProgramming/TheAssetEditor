using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.ToolCreation;
using System.Windows.Controls;
using System.Windows;

namespace Shared.Ui.Common.DataTemplates
{
    public interface IViewProviderGeneric
    {
        Type ViewType { get; }
    }


    public interface IViewProvider<T> : IViewProviderGeneric
    where T : UserControl
    {
        Type IViewProviderGeneric.ViewType => typeof(T);
    }

    public class ViewTemplateDataSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is IViewProviderGeneric viewProvider)
            {
                var factory = new FrameworkElementFactory(viewProvider.ViewType);
                var dt = new DataTemplate
                {
                    VisualTree = factory
                };
                return dt;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
