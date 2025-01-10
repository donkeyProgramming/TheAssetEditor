using System;
using System.Globalization;
using System.Windows.Data;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.Languages;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Converters
{
    public class SettingsEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;
            else if (value is GameLanguage language)
                return GameLanguageToStringMap[language];
            else if (value is GameSoundBank soundbank)
                return GetDisplayString(soundbank);
            else if (value is DialogueEventPreset dialogueEventSubtype)
                return GetDisplayString(dialogueEventSubtype);
            else if (value is PlaylistType playlistType)
                return PlaylistTypeToStringMap[playlistType];
            else if (value is PlaylistMode playlistMode)
                return PlaylistModeToStringMap[playlistMode];
            else if (value is EndBehaviour endBehaviour)
                return EndBehaviourToStringMap[endBehaviour];
            else if (value is TransitionType transition)
                return TransitionToStringMap[transition];
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
