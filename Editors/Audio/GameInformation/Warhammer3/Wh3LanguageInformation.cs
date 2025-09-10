using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Editors.Audio.GameInformation.Warhammer3
{
    // TODO: Need game-level abstraction for all these game settings (not just languages)
    public enum Wh3GameLanguage
    {
        [Display(Name = "chinese")] Chinese,
        [Display(Name = "english(uk)")] EnglishUK,
        [Display(Name = "french(france)")] FrenchFrance,
        [Display(Name = "german")] German,
        [Display(Name = "italian")] Italian,
        [Display(Name = "polish")] Polish,
        [Display(Name = "russian")] Russian,
        [Display(Name = "spanish(spain)")] SpanishSpain,
        // SoundBanks with sfx as the language are stored directly in the wwise folder as they're used by all languages
        [Display(Name = "sfx")] Sfx 
    }

    public static class Wh3LanguageInformation
    {
        public static string GetGameLanguageAsString(this Wh3GameLanguage language)
        {
            var member = typeof(Wh3GameLanguage)
                .GetMember(language.ToString())
                .FirstOrDefault();
            var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
                return displayAttribute?.GetName();
            else
                return language.ToString();
        }
    }
}
