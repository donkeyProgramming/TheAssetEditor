namespace Editors.Audio.AudioEditor.GameSettings.Warhammer3
{
    public class Languages
    {
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

        public static string GetGameString(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.Chinese => "chinese",
                GameLanguage.EnglishUK => "english(uk)",
                GameLanguage.FrenchFrance => "french(france)",
                GameLanguage.German => "german",
                GameLanguage.Italian => "italian",
                GameLanguage.Polish => "polish",
                GameLanguage.Russian => "russian",
                GameLanguage.SpanishSpain => "spanish(spain)",
            };
        }
    }
}
