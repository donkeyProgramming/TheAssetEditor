using System.Collections.Generic;

namespace Editors.Audio.GameSettings.Warhammer3
{
    public class Languages
    {
        private const string ChineseString = "chinese";
        private const string EnglishUKString = "english(uk)";
        private const string FrenchFranceString = "french(france)";
        private const string GermanString = "german";
        private const string ItalianString = "italian";
        private const string PolishString = "polish";
        private const string RussianString = "russian";
        private const string SpanishSpainString = "spanish(spain)";

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
            { GameLanguage.Chinese, ChineseString },
            { GameLanguage.EnglishUK, EnglishUKString },
            { GameLanguage.FrenchFrance, FrenchFranceString },
            { GameLanguage.German, GermanString },
            { GameLanguage.Italian, ItalianString },
            { GameLanguage.Polish, PolishString },
            { GameLanguage.Russian, RussianString },
            { GameLanguage.SpanishSpain, SpanishSpainString }
        };
    }
}
