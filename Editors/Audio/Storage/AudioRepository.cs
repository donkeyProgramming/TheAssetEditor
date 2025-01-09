using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, string> NameLookUpTable { get; }
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
        public Dictionary<uint, List<HircItem>> HircObjects { get; private set; } = [];
        public Dictionary<uint, List<DidxAudio>> DidxAudioObject { get; internal set; }
        public Dictionary<string, PackFile> PackFileMap { get; private set; }
        public Dictionary<uint, string> NameLookUpTable { get; private set; } = [];
        public Dictionary<string, List<string>> DialogueEventsWithStateGroups { get; private set; }
        public Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiersAndStateGroups { get; set; } = [];
        public Dictionary<string, List<string>> StateGroupsWithStates { get; private set; }

        public AudioRepository(RepositoryProvider provider, ApplicationSettingsService applicationSettingsService)
        {
            var audioData = new AudioData();

            var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
            var gameBankGeneratorVersion = gameInformation.BankGeneratorVersion;
            if (gameBankGeneratorVersion != GameBnkVersion.Unsupported)
            {
                provider.LoadDatData(audioData);
                provider.LoadBnkData(audioData);
            }

            HircObjects = audioData.HircObjects ?? [];
            DidxAudioObject = audioData.DidxAudioObject ?? [];
            PackFileMap = audioData.PackFileMap ?? [];
            NameLookUpTable = audioData.NameLookUpTable ?? [];
            DialogueEventsWithStateGroups = audioData.DialogueEventsWithStateGroups ?? [];
            DialogueEventsWithStateGroupsWithQualifiersAndStateGroups = audioData.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups ?? [];
            StateGroupsWithStates = audioData.StateGroupsWithStates ?? [];
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (HircObjects.ContainsKey(id))
                return HircObjects[id];
            return [];
        }

        public List<HircItem> GetHircObject(uint id, string owningFileName)
        {
            var hircItems = GetHircObject(id).Where(x => x.OwnerFile == owningFileName).ToList();
            return hircItems;
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
                    if (hircItem.HircType == AkBkHircType.Dialogue_Event && hircItem.Id == id && hircItem.IsCaHircItem)
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
    }
}
