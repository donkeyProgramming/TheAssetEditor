using CommonControls.Services.ToolCreation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AssetEditor.DataTemplates
{
    public class EditorTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var parent = FindParent<TabControl>(container);
            var toolFactory = (IToolFactory)parent.GetValue(ToolFactoryParameter.ViewFactoryProperty);
            var viewType = toolFactory.GetViewTypeFromViewModel(item.GetType());

            FrameworkElementFactory factory = new FrameworkElementFactory(viewType);
            DataTemplate dt = new DataTemplate();
            dt.VisualTree = factory;

            return dt;
        }

        public T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }

    public class ToolFactoryParameter : DependencyObject
    {
        public static readonly DependencyProperty ViewFactoryProperty = DependencyProperty.RegisterAttached("ViewFactory", typeof(IToolFactory), typeof(ToolFactoryParameter));

        public static IToolFactory GetViewFactory(DependencyObject obj)
        {
            return (IToolFactory)obj.GetValue(ViewFactoryProperty);
        }

        public static void SetViewFactory(DependencyObject obj, IToolFactory value)
        {
            obj.SetValue(ViewFactoryProperty, value);
        }
    }
}
