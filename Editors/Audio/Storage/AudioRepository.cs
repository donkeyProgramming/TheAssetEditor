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
        Dictionary<uint, Dictionary<uint, List<HircItem>>> HircLookupByLanguageIDByID { get; }
        Dictionary<uint, Dictionary<uint, List<ICAkSound>>> SoundHircLookupByLanguageIDBySourceID { get; }
        Dictionary<uint, Dictionary<uint, List<DidxAudio>>> DidxAudioLookupByLanguageIDByID { get; }
        Dictionary<uint, List<HircItem>> HircLookupByID { get; }
        Dictionary<uint, List<DidxAudio>> DidxAudioLookupByID { get; }
        Dictionary<string, PackFile> BnkPackFileLookupByName { get; }
        Dictionary<uint, string> NameLookupByID { get; }
        Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; }
        Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; }
        Dictionary<string, List<string>> StatesLookupByStateGroup { get; }
        Dictionary<string, Dictionary<uint, string>> StatesLookupByStateGroupByStateID { get; }

        void ExportNameListToFile(string outputDirectory, bool includeIds = false);
        List<T> GetHircItemsByType<T>() where T : class;
        List<HircItem> GetHircObject(uint id);
        List<HircItem> GetHircObject(uint id, string owningFileName);
        string GetNameFromID(uint value);
        string GetNameFromID(uint value, out bool found);
        string GetNameFromID(uint? key);
        string GetOwnerFileFromDialogueEvent(uint id);
        string GetStateGroupFromStateGroupWithQualifier(string dialogueEvent, string stateGroupWithQualifier);
    }

    public class AudioRepository : IAudioRepository
    {
        public Dictionary<uint, Dictionary<uint, List<HircItem>>> HircLookupByLanguageIDByID { get; set; }
        public Dictionary<uint, Dictionary<uint, List<ICAkSound>>> SoundHircLookupByLanguageIDBySourceID { get; set; }
        public Dictionary<uint, Dictionary<uint, List<DidxAudio>>> DidxAudioLookupByLanguageIDByID { get; set; }
        public Dictionary<uint, List<HircItem>> HircLookupByID { get; set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioLookupByID { get; set; }
        public Dictionary<string, PackFile> BnkPackFileLookupByName { get; set; }
        public Dictionary<uint, string> NameLookupByID { get; set; }
        public Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; set; }
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; set; }
        public Dictionary<string, List<string>> StatesLookupByStateGroup { get; set; }
        public Dictionary<string, Dictionary<uint, string>> StatesLookupByStateGroupByStateID { get; set; }

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

            HircLookupByLanguageIDByID = audioData.HircLookupByLanguageIDByID ?? [];
            SoundHircLookupByLanguageIDBySourceID = audioData.SoundHircLookupByLanguageIDBySourceID ?? [];
            DidxAudioLookupByLanguageIDByID = audioData.DidxAudioLookupByLanguageIDByID ?? [];
            HircLookupByID = audioData.HircLookupByID ?? [];
            DidxAudioLookupByID = audioData.DidxAudioLookupByID ?? [];
            BnkPackFileLookupByName = audioData.BnkPackFileLookupByName ?? [];
            NameLookupByID = audioData.NameLookupByID ?? [];
            StateGroupsLookupByDialogueEvent = audioData.StateGroupsLookupByDialogueEvent ?? [];
            QualifiedStateGroupLookupByStateGroupByDialogueEvent = audioData.QualifiedStateGroupLookupByStateGroupByDialogueEvent ?? [];
            StatesLookupByStateGroup = audioData.StatesLookupByStateGroup ?? [];
            StatesLookupByStateGroupByStateID = audioData.StatesLookupByStateGroupByStateID ?? [];
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (HircLookupByID.ContainsKey(id))
                return HircLookupByID[id];
            return [];
        }

        public List<HircItem> GetHircObject(uint id, string owningFileName)
        {
            var hircItems = GetHircObject(id).Where(x => x.OwnerFilePath == owningFileName).ToList();
            return hircItems;
        }

        public string GetNameFromID(uint value, out bool found)
        {
            found = NameLookupByID.ContainsKey(value);
            if (found)
                return NameLookupByID[value];
            return value.ToString();
        }

        public List<T> GetHircItemsByType<T>() where T : class
        {
            return HircLookupByID.Values
                .SelectMany(items => items)
                .OfType<T>()
                .ToList();
        }

        public string GetNameFromID(uint value) => GetNameFromID(value, out var _);

        public void ExportNameListToFile(string outputDirectory, bool includeIds = false)
        {
            var ss = new StringBuilder();

            foreach (var item in NameLookupByID)
            {
                if (includeIds)
                    ss.AppendLine($"{item.Key}\t\t{item.Value}");
                else
                    ss.AppendLine($"{item.Value}");
            }

            var path = Path.Combine(outputDirectory, "AudioNames.wwiseids");
            File.WriteAllText(path, ss.ToString());
        }

        public string GetNameFromID(uint? key)
        {
            if (key.HasValue)
                return GetNameFromID(key.Value);
            else
                throw new System.NotImplementedException();
        }

        public string GetOwnerFileFromDialogueEvent(uint id)
        {
            if (HircLookupByID.TryGetValue(id, out var hircItemList))
            {
                foreach (var hircItem in hircItemList)
                {
                    if (hircItem.HircType == AkBkHircType.Dialogue_Event && hircItem.ID == id && hircItem.IsCaHircItem)
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
