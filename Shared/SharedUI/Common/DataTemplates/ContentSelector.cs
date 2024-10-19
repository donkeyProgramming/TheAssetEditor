using Shared.Core.ToolCreation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Shared.Ui.Common.DataTemplates
{
    public class EditorTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            
            if(item == null)
                return base.SelectTemplate(item, container);

            var parent = FindParent<TabControl>(container);
            var toolFactory = (IEditorDatabase)parent.GetValue(ToolFactoryParameter.ViewFactoryProperty);
            var viewType = toolFactory.GetViewTypeFromViewModel(item.GetType());

            var factory = new FrameworkElementFactory(viewType);
            var dt = new DataTemplate
            {
                VisualTree = factory
            };

            return dt;
        }

        public T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            var parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }

    public class ToolFactoryParameter : DependencyObject
    {
        public static readonly DependencyProperty ViewFactoryProperty = DependencyProperty.RegisterAttached("ViewFactory", typeof(IEditorDatabase), typeof(ToolFactoryParameter));

        public static IEditorDatabase GetViewFactory(DependencyObject obj)
        {
            return (IEditorDatabase)obj.GetValue(ViewFactoryProperty);
        }

        public static void SetViewFactory(DependencyObject obj, IEditorDatabase value)
        {
            obj.SetValue(ViewFactoryProperty, value);
        }
    }
}
