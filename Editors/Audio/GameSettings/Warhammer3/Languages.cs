using System.Collections.Generic;

namespace Editors.Audio.GameSettings.Warhammer3
{
    // TODO: need abstraction for selected game
    public class Languages
    {
        public static string Chinese { get; } = "chinese";
        public static string EnglishUK { get; } = "english(uk)";
        public static string FrenchFrance { get; } = "french(france)";
        public static string German { get; } = "german";
        public static string Italian { get; } = "italian";
        public static string Polish { get; } = "polish";
        public static string Russian { get; } = "russian";
        public static string SpanishSpain { get; } = "spanish(spain)";
        public static string Sfx { get; } = "sfx"; // SoundBanks with sfx as the language are stored directly in the wwise folder as they're used by all languages.

        public enum GameLanguage
        {
            Chinese,
            EnglishUK,
            FrenchFrance,
            German,
            Italian,
            Polish,
            Russian,
            SpanishSpain
        }

        public static readonly Dictionary<GameLanguage, string> GameLanguageToStringMap = new()
        {
            { GameLanguage.Chinese, Chinese },
            { GameLanguage.EnglishUK, EnglishUK },
            { GameLanguage.FrenchFrance, FrenchFrance },
            { GameLanguage.German, German },
            { GameLanguage.Italian, Italian },
            { GameLanguage.Polish, Polish },
            { GameLanguage.Russian, Russian },
            { GameLanguage.SpanishSpain, SpanishSpain }
        };
    }
}
