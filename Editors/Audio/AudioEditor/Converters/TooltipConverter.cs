using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Editors.Audio.AudioEditor.Converters
{
    public class TooltipConverter
    {
        public class ConvertTooltipCollectionToString : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is IEnumerable<string> collection)
                    return string.Join(", ", collection.Select(item => $"\"{item}\""));
                else if (value is IList<string> list)
                    return string.Join(", ", list.Select(item => $"\"{item}\""));
                else if (value is IEnumerable enumerable)
                {
                    var stringValue = new StringBuilder();
                    foreach (var item in enumerable)
                    {
                        stringValue.Append($"\"{item.ToString()}\"");
                        stringValue.Append(", ");
                    }
                    return stringValue.ToString().TrimEnd([',', ' ']);
                }
                return string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }
    }
}
