using System.Windows;
using System.Windows.Media;

namespace MonoGame.Framework.WpfInterop.Internals
{
    public static class LogicalTreeHelperEx
    {
        /// <summary>
        /// Gets the parent of a specific type that hosts the specific child.
        /// Returns null if no match is found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (true)
            {
                //get parent item
                // this one works when setting tabs explicitly, but breaks on <TabControl ItemsSource={Binding ...}" />
                DependencyObject parentObject = LogicalTreeHelper.GetParent(child);
                // this one does not work when set explicitely, but works when using ItemsSource bindings
                DependencyObject parentObject2 = VisualTreeHelper.GetParent(child);

                //we've reached the end of the tree
                if (parentObject == null && parentObject2 == null)
                    return null;

                //check if the parent matches the type we're looking for
                var parent = parentObject as T ?? parentObject2 as T;
                if (parent != null)
                    return parent;
                child = parentObject ?? parentObject2;
            }
        }
    }
}