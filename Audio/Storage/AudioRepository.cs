using Audio.FileFormats.WWise;
using System.Collections.Generic;

namespace Audio.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, string> NameLookUpTable { get; }
        Dictionary<uint, List<HircItem>> HircObjects { get; }

        List<HircItem> GetHircObject(uint id);
        string GetNameFromHash(uint value);
        string GetNameFromHash(uint value, out bool found);
    }

    public class AudioRepository : IAudioRepository
    {
        public Dictionary<uint, string> NameLookUpTable { get; private set; } = new Dictionary<uint, string>();
        public Dictionary<uint, List<HircItem>> HircObjects { get; private set; } = new Dictionary<uint, List<HircItem>>();

        public AudioRepository(RepositoryProvider provider)
        {
            var data = provider.Load();
            NameLookUpTable = data.NameLookup;
            HircObjects = data.HircObjects;
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (HircObjects.ContainsKey(id))
                return HircObjects[id];

            return new List<HircItem>();
        }


        // Name lookup
        public string GetNameFromHash(uint value, out bool found)
        {
            found = NameLookUpTable.ContainsKey(value);
            if (found)
                return NameLookUpTable[value];
            return value.ToString();
        }

        public string GetNameFromHash(uint value) => GetNameFromHash(value, out var _);
    }

}
