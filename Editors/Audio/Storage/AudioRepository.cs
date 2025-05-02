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
        Dictionary<uint, Dictionary<uint, List<HircItem>>> HircLookupByLanguageIdById { get; }
        Dictionary<uint, Dictionary<uint, List<ICAkSound>>> SoundHircLookupByLanguageIdBySourceId { get; }
        Dictionary<uint, Dictionary<uint, List<DidxAudio>>> DidxAudioLookupByLanguageIdById { get; }
        Dictionary<uint, List<HircItem>> HircLookupById { get; }
        Dictionary<uint, List<DidxAudio>> DidxAudioLookupById { get; }
        Dictionary<string, PackFile> BnkPackFileLookupByName { get; }
        Dictionary<uint, string> NameLookupById { get; }
        Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; }
        Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; }
        Dictionary<string, List<string>> StatesLookupByStateGroup { get; }
        Dictionary<string, Dictionary<uint, string>> StatesLookupByStateGroupByStateId { get; }

        void ExportNameListToFile(string outputDirectory, bool includeIds = false);
        List<T> GetHircItemsByType<T>() where T : class;
        List<HircItem> GetHircObject(uint id);
        List<HircItem> GetHircObject(uint id, string owningFileName);
        string GetNameFromId(uint value);
        string GetNameFromId(uint value, out bool found);
        string GetNameFromId(uint? key);
        string GetOwnerFileFromDialogueEvent(uint id);
        string GetStateGroupFromStateGroupWithQualifier(string dialogueEvent, string stateGroupWithQualifier);
    }

    public class AudioRepository : IAudioRepository
    {
        public Dictionary<uint, Dictionary<uint, List<HircItem>>> HircLookupByLanguageIdById { get; set; }
        public Dictionary<uint, Dictionary<uint, List<ICAkSound>>> SoundHircLookupByLanguageIdBySourceId { get; set; }
        public Dictionary<uint, Dictionary<uint, List<DidxAudio>>> DidxAudioLookupByLanguageIdById { get; set; }
        public Dictionary<uint, List<HircItem>> HircLookupById { get; set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioLookupById { get; set; }
        public Dictionary<string, PackFile> BnkPackFileLookupByName { get; set; }
        public Dictionary<uint, string> NameLookupById { get; set; }
        public Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; set; }
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; set; }
        public Dictionary<string, List<string>> StatesLookupByStateGroup { get; set; }
        public Dictionary<string, Dictionary<uint, string>> StatesLookupByStateGroupByStateId { get; set; }

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

            HircLookupByLanguageIdById = audioData.HircLookupByLanguageIdById ?? [];
            SoundHircLookupByLanguageIdBySourceId = audioData.SoundHircLookupByLanguageIdBySourceId ?? [];
            DidxAudioLookupByLanguageIdById = audioData.DidxAudioLookupByLanguageIdById ?? [];
            HircLookupById = audioData.HircLookupById ?? [];
            DidxAudioLookupById = audioData.DidxAudioLookupById ?? [];
            BnkPackFileLookupByName = audioData.BnkPackFileLookupByName ?? [];
            NameLookupById = audioData.NameLookupById ?? [];
            StateGroupsLookupByDialogueEvent = audioData.StateGroupsLookupByDialogueEvent ?? [];
            QualifiedStateGroupLookupByStateGroupByDialogueEvent = audioData.QualifiedStateGroupLookupByStateGroupByDialogueEvent ?? [];
            StatesLookupByStateGroup = audioData.StatesLookupByStateGroup ?? [];
            StatesLookupByStateGroupByStateId = audioData.StatesLookupByStateGroupByStateId ?? [];
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (HircLookupById.ContainsKey(id))
                return HircLookupById[id];
            return [];
        }

        public List<HircItem> GetHircObject(uint id, string owningFileName)
        {
            var hircItems = GetHircObject(id).Where(x => x.OwnerFilePath == owningFileName).ToList();
            return hircItems;
        }

        public string GetNameFromId(uint value, out bool found)
        {
            found = NameLookupById.ContainsKey(value);
            if (found)
                return NameLookupById[value];
            return value.ToString();
        }

        public List<T> GetHircItemsByType<T>() where T : class
        {
            return HircLookupById.Values
                .SelectMany(items => items)
                .OfType<T>()
                .ToList();
        }

        public string GetNameFromId(uint value) => GetNameFromId(value, out var _);

        public void ExportNameListToFile(string outputDirectory, bool includeIds = false)
        {
            var ss = new StringBuilder();

            foreach (var item in NameLookupById)
            {
                if (includeIds)
                    ss.AppendLine($"{item.Key}\t\t{item.Value}");
                else
                    ss.AppendLine($"{item.Value}");
            }

            var path = Path.Combine(outputDirectory, "AudioNames.wwiseids");
            File.WriteAllText(path, ss.ToString());
        }

        public string GetNameFromId(uint? key)
        {
            if (key.HasValue)
                return GetNameFromId(key.Value);
            else
                throw new System.NotImplementedException();
        }

        public string GetOwnerFileFromDialogueEvent(uint id)
        {
            if (HircLookupById.TryGetValue(id, out var hircItemList))
            {
                foreach (var hircItem in hircItemList)
                {
                    if (hircItem.HircType == AkBkHircType.Dialogue_Event && hircItem.Id == id && hircItem.IsCaHircItem)
                    {
                        var file = Path.GetFileName(hircItem.OwnerFilePath);
                        file = Path.GetFileNameWithoutExtension(file);
                        file = file.Replace("__core", string.Empty);

                        return file;
                    }
                }
            }
            return null;
        }

        public string GetStateGroupFromStateGroupWithQualifier(string dialogueEvent, string stateGroupWithQualifier)
        {
            if (QualifiedStateGroupLookupByStateGroupByDialogueEvent.TryGetValue(dialogueEvent, out var stateGroupDictionary))
                if (stateGroupDictionary.TryGetValue(stateGroupWithQualifier, out var stateGroup))
                    return stateGroup;

            return null;
        }
    }
}
