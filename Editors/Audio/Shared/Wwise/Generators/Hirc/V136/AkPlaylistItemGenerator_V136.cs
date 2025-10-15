using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;
using static Shared.GameFormats.Wwise.Hirc.V136.CAkRanSeqCntr_V136.CAkPlayList_V136;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    public class AkPlaylistItemGenerator_V136
    {
        public static List<AkPlaylistItem_V136> CreateAkPlaylistItem(List<Sound> sounds)
        {
            var playlist = new List<AkPlaylistItem_V136>();

            // We order them in the order they should play sequentially (were there no random settings etc.), but for some reason beyond me
            // the order of Sounds is sometimes not what it says it is in the Audio Project so we order them by PlaylistOrder to be safe.
            var orderedSounds = sounds
                .OrderBy(sound => sound.PlaylistOrder)
                .ToList();

            foreach (var sound in orderedSounds)
            {
                var playlistItem = new AkPlaylistItem_V136
                {
                    PlayId = sound.Id,
                    Weight = 50000
                };
                playlist.Add(playlistItem);
            }

            return playlist;
        }
    }
}
