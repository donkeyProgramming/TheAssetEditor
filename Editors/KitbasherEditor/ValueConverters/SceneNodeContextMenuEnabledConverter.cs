using System;
using System.Windows.Data;
using View3D.SceneNodes;

namespace KitbasherEditor.ValueConverters
{
    public enum NodeConvertionMode
    {
        MakeEditable,
        DeleteNode
    }

    [ValueConversion(typeof(SceneNode), typeof(bool))]
    public class SceneNodeContextMenuEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SceneNode node = value as SceneNode;
            if (node.Name == "Root")
                return false;
            if (node.Name == "Editable Model")
                return false;
            if (node.Name == "Reference meshs")
                return false;

            if (node is Rmv2LodNode)
            {
                if (node.Parent.Name == "Editable Model")
                    return false;
            }

            var enumValue = Enum.Parse<NodeConvertionMode>(parameter.ToString());
            if (enumValue == NodeConvertionMode.MakeEditable)
            {
                if (node.IsEditable == false)
                {
                    if (node is Rmv2ModelNode)
                        return true;
                    // if (node is Rmv2LodNode)
                    //     return true;
                    if (node is Rmv2MeshNode)
                        return true;

                    return false;
                }
                return false;
            }
            // var res = Enum.TryParse
            //     
            //     
            //     (typeof(NodeConvertionMode), parameter.ToString(), true, out var enumValue);
            //
            return true;
        }


        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
