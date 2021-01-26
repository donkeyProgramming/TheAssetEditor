using CommonControls.Resources;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AssetEditor.ViewModels.FileTreeView
{
    [ValueConversion(typeof(Node), typeof(BitmapImage))]
    public class PackFileToImageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is PackFileContainer)
                return ResourceController.CollectionIcon;
            else if (value is PackFile)
                return ResourceController.FileIcon;
            else if (value is PackFileDirectory)
                return ResourceController.FolderIcon;

            throw new Exception("Unknown type " + value.GetType().FullName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
