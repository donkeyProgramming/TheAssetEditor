using System;
using System.Globalization;
using System.Windows.Data;
using Editors.Audio.GameSettings.Warhammer3;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.Converters
{
    public class SettingsEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;
            else if (value is Languages.GameLanguage language)
                return Languages.GameLanguageToStringMap[language];
            else if (value is SoundBanks.GameSoundBank soundbank)
                return SoundBanks.GetDisplayString(soundbank);
            else if (value is DialogueEvents.DialogueEventPreset dialogueEventSubtype)
                return DialogueEvents.GetDisplayString(dialogueEventSubtype);
            else if (value is PlayType playType)
                return GetPlayTypeString(playType);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
