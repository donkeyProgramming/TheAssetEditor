using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Editors.Audio.Shared.AudioProject.Models
{
    /// <summary>
    /// One culture the music project wires into the music scripts - what the Add Culture wizard
    /// was given for it, kept so the scripts can be rebuilt from vanilla with every culture in
    /// the project rather than patched one culture at a time. A culture with several subcultures
    /// has one of these per subculture, sharing an <see cref="AudioState"/>.
    ///
    /// The project is meant to be kept by whoever maintains the audio mixer mod: a culture mod's
    /// team asks for their culture, the maintainer adds it here with the team's wavs, and the
    /// whole project is rebuilt.
    /// </summary>
    public class MusicCulture
    {
        /// <summary>The culture's audio_state, as typed. battle_music.dat matches it lower-cased,
        /// and it names the culture's events - see <see cref="MusicalCulture"/>.</summary>
        public string AudioState { get; set; }

        /// <summary>The subculture key campaign_music.dat matches, e.g. ovn_sc_arb_araby.</summary>
        public string SubcultureKey { get; set; }

        /// <summary>The slots this culture is wired into. A slot left out of the wizard is not
        /// listed.</summary>
        public List<MusicCultureSlot> Slots { get; set; } = [];

        /// <summary>The Audio State as the scripts match it and as event names are built from it:
        /// trimmed and lower-cased, since the database has it both ways.</summary>
        [JsonIgnore] public string MusicalCulture => AudioState?.Trim().ToLowerInvariant() ?? "";

        /// <summary>The events the scripts post for this culture that the project has to hold - one
        /// per slot that brings its own audio. A borrowing slot posts the borrowed culture's event,
        /// which already exists.</summary>
        public IEnumerable<string> GetOwnEventNames() => Slots
            .Where(slot => slot.ReferenceMatchKey == null)
            .Select(slot => slot.EventPrefix + MusicalCulture);
    }

    public class MusicCultureSlot
    {
        /// <summary>The event family the slot posts, e.g. music_b_faction_.</summary>
        public string EventPrefix { get; set; }

        /// <summary>An existing culture whose arm is copied, so this culture plays that culture's
        /// audio; null when the culture brings its own and the project holds its event.</summary>
        public string ReferenceMatchKey { get; set; }
    }
}
