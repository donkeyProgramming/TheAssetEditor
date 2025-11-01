using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Shared.EmbeddedResources;
using Editors.Audio.AudioEditor.Presentation.Shared;

namespace Editors.Audio.Shared.UI.Converters
{
    [ValueConversion(typeof(AudioFilesTreeNode), typeof(BitmapImage))]
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is AudioFilesTreeNode node)
            {
                if (node.Type == AudioFilesTreeNodeType.Directory)
                    return IconLibrary.FolderIcon;
                else if (node.Type == AudioFilesTreeNodeType.WavFile)
                    return IconLibrary.AudioFileIcon;
            }

            throw new Exception("Unknown type " + value.GetType().FullName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
