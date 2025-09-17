using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using static Shared.GameFormats.Wwise.Hirc.V136.CAkRanSeqCntr_V136.CAkPlayList_V136;

namespace Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Hirc.V136
{
    public class AkPlaylistItemGenerator_V136
    {
        public static List<AkPlaylistItem_V136> CreateAkPlaylistItem(List<Sound> sounds)
        {
            var playlist = new List<AkPlaylistItem_V136>();
            foreach (var sound in sounds)
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
