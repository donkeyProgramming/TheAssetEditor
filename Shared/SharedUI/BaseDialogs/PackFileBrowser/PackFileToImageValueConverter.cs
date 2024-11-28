using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Shared.EmbeddedResources;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    [ValueConversion(typeof(TreeNode), typeof(BitmapImage))]
    public class PackFileToImageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TreeNode node)
            {
                if (node.GetNodeType() == NodeType.Root)
                    return IconLibrary.CollectionIcon;
                else if (node.GetNodeType() == NodeType.Directory)
                    return IconLibrary.FolderIcon;
                if (node.GetNodeType() == NodeType.File)
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
