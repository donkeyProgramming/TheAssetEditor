using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GameWorld.Core.SceneNodes;

namespace Editors.KitbasherEditor.ValueConverters
{
    public class SceneNodeToLockStateConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var node = values[0] as SceneNode;
            if (node.IsEditable == true)
            {
                if (node is ISelectable selectable)
                {
                    if (selectable.IsSelectable == false)
                    {
                        if (node is Rmv2ModelNode)
                            return Visibility.Visible;
                        if (node is Rmv2MeshNode)
                            return Visibility.Visible;
                    }
                }
                else if (node is GroupNode groupNode)
                {
                    if (groupNode.IsSelectable == false && groupNode.IsLockable)
                        return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;

        }

        public object[] ConvertBack(
            object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
