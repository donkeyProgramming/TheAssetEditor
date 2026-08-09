using System;
using System.Globalization;
using System.Windows.Data;

namespace Editors.CampaignAnimationSetEditor.Views
{
    /// <summary>Shows the tail of a long path (the filename, the meaningful part) rather than
    /// trimming the end as WPF does by default. Applied through DataGridTemplateColumn's
    /// CellTemplate, never CellEditingTemplate - DataGridTextColumn shares one binding between its
    /// read-only and editing views, so shortening there would corrupt the value being edited.</summary>
    public class PathTailConverter : IValueConverter
    {
        const int MaxLength = 55;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? "";
            if (text.Length <= MaxLength)
                return text;

            return "…" + text[(text.Length - (MaxLength - 1))..];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
