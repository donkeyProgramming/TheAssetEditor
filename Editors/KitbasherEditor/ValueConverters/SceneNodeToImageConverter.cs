using GameWorld.Core.SceneNodes;
using Shared.EmbeddedResources;
using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace KitbasherEditor.ValueConverters
{
    [ValueConversion(typeof(SceneNode), typeof(BitmapImage))]
    public class SceneNodeToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is VariantMeshNode)
                return IconLibrary.VmdIcon;
            else if (value is Rmv2ModelNode)
                return IconLibrary.Rmv2ModelIcon;
            else if (value is Rmv2MeshNode)
                return IconLibrary.MeshIcon;
            else if (value is SkeletonNode)
                return IconLibrary.SkeletonIcon;
            else if (value is GroupNode)
                return IconLibrary.GroupIcon;


            throw new Exception("Unknown type " + value.GetType().FullName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
