using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Shared.EmbeddedResources;

namespace Shared.Ui.BaseDialogs.PackFileTree.ValueConverters
{
    [ValueConversion(typeof(TreeNode), typeof(BitmapImage))]
    public class PackFileToImageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TreeNode node)
            {
                if (node.NodeType == NodeType.Root)
                    return IconLibrary.CollectionIcon;
                else if (node.NodeType == NodeType.Directory)
                    return IconLibrary.FolderIcon;
                if (node.NodeType == NodeType.File)
                    return IconLibrary.FileIcon;
            }

            throw new Exception("Unknown type " + value.GetType().FullName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
