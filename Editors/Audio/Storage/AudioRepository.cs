using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;
using Shared.GameFormats.WWise;

namespace Editors.Audio.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, string> NameLookUpTable { get; }
        List<SoundDatFile.DatDialogueEventsWithStateGroups> DatDialogueEventsWithStateGroups { get; }
        List<SoundDatFile.DatStateGroupsWithStates> DatStateGroupsWithStates { get; }
        Dictionary<string, List<string>> DialogueEventsWithStateGroups { get; }
        Dictionary<string, List<string>> StateGroupsWithStates { get; }
        Dictionary<uint, List<HircItem>> HircObjects { get; }
        Dictionary<string, PackFile> PackFileMap { get; }

        void ExportNameListToFile(string outputDirectory, bool includeIds = false);
        List<T> GetAllOfType<T>() where T : HircItem;
        List<HircItem> GetHircObject(uint id);
        List<HircItem> GetHircObject(uint id, string owningFileName);
        string GetNameFromHash(uint value);
        string GetNameFromHash(uint value, out bool found);
        string GetNameFromHash(uint? key);
        string GetOwnerFileFromDialogueEvent(uint id);
    }

    public class AudioRepository : IAudioRepository
    {
        public Dictionary<uint, string> NameLookUpTable { get; private set; } = [];
        public List<SoundDatFile.DatDialogueEventsWithStateGroups> DatDialogueEventsWithStateGroups { get; private set; } = [];
        public List<SoundDatFile.DatStateGroupsWithStates> DatStateGroupsWithStates { get; private set; } = [];
        public Dictionary<string, List<string>> DialogueEventsWithStateGroups { get; private set; }
        public Dictionary<string, List<string>> StateGroupsWithStates { get; private set; }
        public Dictionary<uint, List<HircItem>> HircObjects { get; private set; } = [];
        public Dictionary<string, PackFile> PackFileMap { get; private set; }


        public AudioRepository(RepositoryProvider provider)
        {
            {
                var data = provider.LoadBnkAndDatData();
                NameLookUpTable = data.NameLookUpTable;
                DatDialogueEventsWithStateGroups = data.DialogueEventsWithStateGroups;
                DatStateGroupsWithStates = data.StateGroupsWithStates;
                HircObjects = data.HircObjects;
                PackFileMap = data.PackFileMap;

                StoreDialogueEventsWithStateGroups();
                StoreStateGroupsWithStates();
            }
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

        public string GetNameFromHash(uint? key)
        {
            if (key.HasValue)
                return GetNameFromHash(key.Value);
            else
                throw new System.NotImplementedException();
        }

        public string GetOwnerFileFromDialogueEvent(uint id)
        {
            if (HircObjects.TryGetValue(id, out var hircItemList))
            {
                foreach (var hircItem in hircItemList)
                {
                    if (hircItem.Type == HircType.Dialogue_Event && hircItem.Id == id && hircItem.IsCaHircItem)
                    {
                        var file = Path.GetFileName(hircItem.OwnerFile);
                        file = Path.GetFileNameWithoutExtension(file);
                        file = file.Replace("__core", string.Empty);

                        return file;
                    }
                }
            }
            return null;
        }

        public void StoreDialogueEventsWithStateGroups()
        {
            DialogueEventsWithStateGroups = new Dictionary<string, List<string>>();
            var dialogueEventsWithStateGroups = DatDialogueEventsWithStateGroups;

            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                if (!DialogueEventsWithStateGroups.ContainsKey(dialogueEvent.EventName))
                    DialogueEventsWithStateGroups[dialogueEvent.EventName] = new List<string>();

                foreach (var stateGroupId in dialogueEvent.StateGroups)
                {
                    var stateGroup = GetNameFromHash(stateGroupId);
                    DialogueEventsWithStateGroups[dialogueEvent.EventName].Add(stateGroup);
                }
            }
        }

        public void StoreStateGroupsWithStates()
        {
            StateGroupsWithStates = new Dictionary<string, List<string>>();
            var stateGroupsWithStates = DatStateGroupsWithStates;

            foreach (var stateGroup in stateGroupsWithStates)
            {
                if (!StateGroupsWithStates.ContainsKey(stateGroup.StateGroupName))
                    StateGroupsWithStates[stateGroup.StateGroupName] = new List<string>();

                foreach (var state in stateGroup.States)
                    StateGroupsWithStates[stateGroup.StateGroupName].Add(state);
            }
        }
    }
}
