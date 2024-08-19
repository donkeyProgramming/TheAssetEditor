using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

namespace Editors.Audio.AudioEditor
{
    public class SettingsEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return value;

            if (value is Language language)
                return LanguageEnumToString.TryGetValue(language, out var displayString) ? displayString : language.ToString();

            if (value is DialogueEventType dialogueEventType)
                return DialogueEventTypeEnumToString.TryGetValue(dialogueEventType, out var displayString) ? displayString : dialogueEventType.ToString();

            if (value is DialogueEventSubtype dialogueEventSubtype)
                return DialogueEventSubtypeEnumToString.TryGetValue(dialogueEventSubtype, out var displayString) ? displayString : dialogueEventSubtype.ToString();

            if (value is EventType eventSubtype)
                return EventSubtypeEnumToString.TryGetValue(eventSubtype, out var displayString) ? displayString : eventSubtype.ToString();

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public static Language GetLanguageEnumString(string languageString)
        {
            return LanguageEnumToString.FirstOrDefault(pair => pair.Value.Equals(languageString, StringComparison.OrdinalIgnoreCase)).Key;
        }
    }
}
