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
                return GameLanguageStringLookup[language];
            else if (value is Wh3SoundBankSubtype soundBank)
                return GetSoundBankSubTypeString(soundBank);
            else if (value is DialogueEventPreset dialogueEventSubtype)
                return GetDialogueEventPresetDisplayString(dialogueEventSubtype);
            else if (value is PlaylistType playlistType)
                return PlaylistTypeStringLookup[playlistType];
            else if (value is PlaylistMode playlistMode)
                return PlaylistModeStringLookup[playlistMode];
            else if (value is EndBehaviour endBehaviour)
                return EndBehaviourStringLookup[endBehaviour];
            else if (value is LoopingType loopingType)
                return LoopingTypeStringLookup[loopingType];
            else if (value is TransitionType transitionType)
                return TransitionTypeStringLookup[transitionType];
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
