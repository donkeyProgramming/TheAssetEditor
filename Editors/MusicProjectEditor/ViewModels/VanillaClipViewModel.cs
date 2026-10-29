using Editors.Audio.Shared.AudioProject;

namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>One file vanilla plays for a culture's slot, as the reference panel lists it.</summary>
    public class VanillaClipViewModel(VanillaMusicClip clip)
    {
        public VanillaMusicClip Clip { get; } = clip;

        public string Where => Clip.Where;
        public string Length => Clip.DurationMs is double ms ? FormatSeconds(ms) : "?";

        public string Shared => Clip.AlsoUsedBy.Count switch
        {
            0 => "",
            <= 3 => string.Join(", ", Clip.AlsoUsedBy),
            _ => $"{string.Join(", ", Clip.AlsoUsedBy.Take(3))} +{Clip.AlsoUsedBy.Count - 3}"
        };

        public string SharedToolTip => Clip.AlsoUsedBy.Count == 0
            ? "Only this culture plays this file."
            : $"Also played for: {string.Join(", ", Clip.AlsoUsedBy)}";

        public static string FormatSeconds(double ms)
        {
            var seconds = ms / 1000;
            return seconds < 60 ? $"{seconds:0.0} s" : $"{(int)(seconds / 60)}:{seconds % 60:00} min";
        }
    }
}
