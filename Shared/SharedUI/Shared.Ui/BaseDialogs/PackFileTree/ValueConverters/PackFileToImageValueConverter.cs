using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Shared.Core.PackFiles.Models;
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
                    return GetRootIcon(node as RootTreeNode);
                else if (node.NodeType == NodeType.Directory)
                    return IconLibrary.FolderIcon;
                if (node.NodeType == NodeType.File)
                    return IconLibrary.FileIcon;
            }

            throw new Exception("Unknown type " + value.GetType().FullName);
        }

        private static BitmapImage GetRootIcon(RootTreeNode? root)
        {
            var container = root?.Owner;

            switch(container!.ContainerType)
            {
                case PackFileContainerType.Normal:
                    return IconLibrary.NormalModPackIcon;
                case PackFileContainerType.Database:
                    return IconLibrary.DatabaseModPackIcon;
                case PackFileContainerType.SystemFolder:
                    return IconLibrary.SystemFolderModPackIcon;
                default:
                    return IconLibrary.MissingIcon;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
