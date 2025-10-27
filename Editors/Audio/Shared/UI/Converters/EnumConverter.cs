using System;
using System.Globalization;
using System.Windows.Data;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise;

namespace Editors.Audio.Shared.UI.Converters
{
    public class EnumConverter : IValueConverter
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
            else if (value is ContainerType containerType)
                return HircSettings.GetEnumDisplayName(containerType);
            else if (value is RandomType randomType)
                return HircSettings.GetEnumDisplayName(randomType);
            else if (value is PlayMode containerMode)
                return HircSettings.GetEnumDisplayName(containerMode);
            else if (value is PlaylistEndBehaviour endBehaviour)
                return HircSettings.GetEnumDisplayName(endBehaviour);
            else if (value is LoopingType loopingType)
                return HircSettings.GetEnumDisplayName(loopingType);
            else if (value is TransitionType transitionType)
                return HircSettings.GetEnumDisplayName(transitionType);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
