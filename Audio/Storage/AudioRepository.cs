﻿using Audio.FileFormats.WWise;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Audio.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, string> NameLookUpTable { get; }
        Dictionary<uint, List<HircItem>> HircObjects { get; }

        void ExportNameListToFile(string outputDirectory, bool includeIds = false);
        List<T> GetAllOfType<T>() where T : HircItem;
        List<HircItem> GetHircObject(uint id);
        List<HircItem> GetHircObject(uint id, string owningFileName);
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
            NameLookUpTable = data.NameLookUpTable;
            HircObjects = data.HircObjects;
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (HircObjects.ContainsKey(id))
                return HircObjects[id];

            return new List<HircItem>();
        }

        public List<HircItem> GetHircObject(uint id, string owningFileName)
        {
            var hircs = GetHircObject(id).Where(x => x.OwnerFile == owningFileName).ToList();
            return hircs;
        }

        // Name lookup
        public string GetNameFromHash(uint value, out bool found)
        {
            found = NameLookUpTable.ContainsKey(value);
            if (found)
                return NameLookUpTable[value];
            return value.ToString();
        }


        public List<T> GetAllOfType<T>() where T : HircItem
        {
            return HircObjects
                .SelectMany(x => x.Value)
                .Select(x => x as T)
                .Where(x => x != null)
                .ToList();
        }

        public string GetNameFromHash(uint value) => GetNameFromHash(value, out var _);

        public void ExportNameListToFile(string outputDirectory, bool includeIds = false)
        {
            var ss = new StringBuilder();

            foreach (var item in NameLookUpTable)
            {
                if (includeIds)
                    ss.AppendLine($"{item.Key}\t\t{item.Value}");
                else
                    ss.AppendLine($"{item.Value}");
            }

            var path = Path.Combine(outputDirectory, "AudioNames.wwiseids");
            File.WriteAllText(path, ss.ToString());
        }


    }

}
