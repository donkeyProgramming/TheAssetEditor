using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Shared.EmbeddedResources;

namespace Editors.Audio.AudioEditor.Presentation.AudioFilesExplorer.ValueConverters
{
    [ValueConversion(typeof(AudioFilesTreeNode), typeof(BitmapImage))]
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
