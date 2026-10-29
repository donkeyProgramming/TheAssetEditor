using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>One culture in the music project, with a row per slot it is wired into.</summary>
    public class MusicCultureViewModel(MusicCulture musicCulture, IReadOnlyList<MusicEventViewModel> events)
    {
        public MusicCulture MusicCulture { get; } = musicCulture;
        public IReadOnlyList<MusicEventViewModel> Events { get; } = events;

        public string AudioState => MusicCulture.AudioState;
        public string SubcultureKey => MusicCulture.SubcultureKey;

        public string Summary
        {
            get
            {
                var own = Events.Count(musicEvent => !musicEvent.IsBorrowed);
                var borrowed = Events.Count - own;
                return borrowed == 0 ? $"{own} own" : $"{own} own, {borrowed} borrowed";
            }
        }
    }
}
