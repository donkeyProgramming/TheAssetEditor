using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Editors.Audio.Shared.GameInformation.Warhammer3
{
    // TODO: Need game-level abstraction for all these game settings (not just languages)
    public enum Wh3Language
    {
        [Display(Name = "chinese")] Chinese,
        [Display(Name = "english(uk)")] EnglishUK,
        [Display(Name = "french(france)")] FrenchFrance,
        [Display(Name = "german")] German,
        [Display(Name = "italian")] Italian,
        [Display(Name = "polish")] Polish,
        [Display(Name = "russian")] Russian,
        [Display(Name = "spanish(spain)")] SpanishSpain,

        // SoundBanks with sfx as the language are stored directly in the "wwise" folder as they're used by all languages
        [Display(Name = "sfx")] Sfx 
    }

    public static class Wh3LanguageInformation
    {
        public static string GetLanguageAsString(Wh3Language language)
        {
            var field = typeof(Wh3Language).GetField(language.ToString());
            var display = field?.GetCustomAttribute<DisplayAttribute>();
            if (display != null)
                return display.GetName();
            return language.ToString();
        }

        public static List<string> GetAllLanguages()
        {
            var languages = new List<string>();
            foreach (Wh3Language language in Enum.GetValues(typeof(Wh3Language)))
                languages.Add(GetLanguageAsString(language));
            return languages;
        }
    }
}
