namespace Editors.Audio.AudioEditor.AudioSettings
{
    public class AudioSettings
    {
        public enum PlayType
        {
            Sequence,
            Random,
            RandomStandard,
            RandomShuffle
        }

        public static string GetPlayTypeString(PlayType playType)
        {
            return playType switch
            {
                PlayType.Sequence => "Sequence",
                PlayType.Random => "Random",
                PlayType.RandomStandard => "Random Standard",
                PlayType.RandomShuffle => "Random Shuffle"
            };
        }
    }
}
