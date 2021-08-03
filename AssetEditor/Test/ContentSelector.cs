using AssetEditor.Services;
using AssetEditor.ViewModels;
using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AssetEditor.Test
{
    public class DeviceTypeSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var parent = FindParent<TabControl>(container);
            var toolFactory = (ToolFactory)parent.GetValue(DataTemplateParameters.ViewFactoryProperty);
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

    public class DataTemplateParameters : DependencyObject
    {
        public static ToolFactory GetViewFactory(DependencyObject obj)
        {
            return (ToolFactory)obj.GetValue(ViewFactoryProperty);
        }

        public static void SetViewFactory(DependencyObject obj, ToolFactory value)
        {
            obj.SetValue(ViewFactoryProperty, value);
        }

        public static readonly DependencyProperty ViewFactoryProperty = DependencyProperty.RegisterAttached("ViewFactory", typeof(ToolFactory), typeof(DataTemplateParameters));
    }
}
