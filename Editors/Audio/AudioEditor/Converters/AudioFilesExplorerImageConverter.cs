using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Editors.Audio.AudioEditor.ViewModels;
using Shared.EmbeddedResources;

namespace Editors.Audio.AudioEditor.Converters
{
    [ValueConversion(typeof(TreeNode), typeof(BitmapImage))]
    public class AudioFilesExplorerImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TreeNode node)
            {
                if (node.NodeType == NodeType.Directory)
                    return IconLibrary.FolderIcon;
                else if (node.NodeType == NodeType.WavFile)
                    return IconLibrary.WavFileIcon;
            }

            throw new Exception("Unknown type " + value.GetType().FullName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
