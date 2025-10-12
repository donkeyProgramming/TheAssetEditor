using System;
using System.Globalization;
using System.Windows.Data;
using Editors.Audio.GameInformation.Warhammer3;
using static Editors.Audio.AudioEditor.Settings.Settings;

namespace Editors.Audio.Shared.UI.Converters
{
    public class SettingsEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;
            else if (value is Wh3Language language)
                return Wh3LanguageInformation.GetLanguageAsString(language);
            else if (value is Wh3DialogueEventType dialogueEventType)
                return Wh3DialogueEventInformation.GetDialogueEventTypeDisplayName(dialogueEventType);
            else if (value is Wh3DialogueEventUnitProfile dialogueEventProfile)
                return Wh3DialogueEventInformation.GetDialogueEventProfileDisplayName(dialogueEventProfile);
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
