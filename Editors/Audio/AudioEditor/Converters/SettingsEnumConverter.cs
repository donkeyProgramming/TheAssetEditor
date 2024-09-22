using System;
using System.Globalization;
using System.Windows.Data;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

namespace Editors.Audio.AudioEditor.Converters
{
    public class SettingsEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;

            else if (value is Language language)
                return GetStringFromLanguage(language);

            else if (value is AudioType EventType)
                return GetStringFromAudioType(EventType);

            else if (value is AudioSubtype dialogueEventSubtype)
                return GetStringFromAudioSubtype(dialogueEventSubtype);

            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
