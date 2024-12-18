using System;
using System.Windows.Data;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views;

namespace KitbasherEditor.ValueConverters
{

    [ValueConversion(typeof(SceneNode), typeof(string))]
    public class SceneNodeToRadioButtonGroupingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var sceneNode = (value as SceneExplorerNode).Content;

            if (sceneNode is Rmv2ModelNode)
                return sceneNode.Parent.Id;

            if (sceneNode is Rmv2LodNode)
                return sceneNode.Parent.Id;

            if (sceneNode is SlotNode)
                return Guid.NewGuid().ToString();

            if (sceneNode is WsModelGroup)
                return sceneNode.Parent.Id;

            return Guid.NewGuid().ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
