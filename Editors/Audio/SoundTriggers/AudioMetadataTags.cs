#nullable enable
using Shared.Core.PackFiles;
using Shared.GameFormats.DB;

namespace Editors.Audio.SoundTriggers
{
    public sealed record AudioTagEvents(string BattleStart, string CampaignStart);

    public sealed class AudioMetadataTags
    {
        readonly Dictionary<string, AudioTagEvents> _tags;

        AudioMetadataTags(Dictionary<string, AudioTagEvents> tags) => _tags = tags;

        public static AudioMetadataTags From(IEnumerable<DbRow> rows)
        {
            var tags = new Dictionary<string, AudioTagEvents>(StringComparer.Ordinal);
            foreach (var row in rows)
                tags.TryAdd(row.Text("key"), new AudioTagEvents(row.Text("sound_event_battle_start"), row.Text("sound_event_campaign_start")));
            return new AudioMetadataTags(tags);
        }

        public static AudioMetadataTags Read(IPackFileService pfs, List<string>? problems = null) =>
            From(DbTables.Read(pfs, "audio_metadata_tags_tables", problems));

        public AudioTagEvents? Find(string tag) => _tags.GetValueOrDefault(tag);

        public int Count => _tags.Count;
    }
}
