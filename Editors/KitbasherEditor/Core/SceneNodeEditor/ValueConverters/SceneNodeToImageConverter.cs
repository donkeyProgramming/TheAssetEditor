using System.Windows.Data;
using System.Windows.Media.Imaging;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.Views;
using Shared.EmbeddedResources;

namespace KitbasherEditor.ValueConverters
{
    [ValueConversion(typeof(SceneNode), typeof(BitmapImage))]
    public class SceneNodeToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var content = (value as SceneExplorerNode)?.Content;

            if (content is VariantMeshNode)
                return IconLibrary.VmdIcon;
            else if (content is Rmv2ModelNode)
                return IconLibrary.Rmv2ModelIcon;
            else if (content is Rmv2MeshNode)
                return IconLibrary.MeshIcon;
            else if (content is SkeletonNode)
                return IconLibrary.SkeletonIcon;
            else if (content is GroupNode)
                return IconLibrary.GroupIcon;


            throw new Exception("Unknown type " + value?.GetType()?.FullName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
