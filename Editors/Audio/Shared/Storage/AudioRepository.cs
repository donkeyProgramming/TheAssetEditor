using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, List<HircItem>> HircsById { get; }
        Dictionary<uint, List<DidxAudio>> DidxAudioListById { get; }
        Dictionary<string, PackFile> PackFileByBnkName { get; }
        Dictionary<uint, string> NameById { get; }
        Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; }
        Dictionary<string, Dictionary<string, string>> QualifiedStateGroupByStateGroupByDialogueEvent { get; }
        Dictionary<string, List<string>> StatesByStateGroup { get; }

        void Load(List<string> languages); 
        void Clear();
        List<T> GetHircsByType<T>() where T : class;
        List<HircItem> GetHircsByHircType(AkBkHircType type);
        List<HircItem> GetHircs(uint id);
        List<HircItem> GetHircs(uint id, string owningFileName);
        string GetNameFromId(uint value);
        string GetNameFromId(uint value, out bool found);
        string GetNameFromId(uint? key);
        HashSet<uint> GetUsedVanillaHircIdsByLanguageId(uint languageId);
        HashSet<uint> GetUsedVanillaSourceIdsByLanguageId(uint languageId);
        Dictionary<string, Dictionary<string, List<HircItem>>> GetVanillaDialogueEventsByBnkByLanguage();
        Dictionary<string, Dictionary<string, List<HircItem>>> GetModdedHircsByBnkByLanguage();
        Dictionary<string, List<HircItem>> GetModdedDialogueEventsByLanguage(List<string> moddedSoundBanks);
        List<string> GetModdedSoundBankFilePaths(string bnkNameSubstring);
    }

    public class AudioRepository(ApplicationSettingsService applicationSettingsService, BnkLoader bnkLoader, DatLoader datLoader) : IAudioRepository, IDisposable
    {
        private readonly ApplicationSettingsService _applicationSettingsService = applicationSettingsService;
        private readonly BnkLoader _bnkLoader = bnkLoader;
        private readonly DatLoader _datLoader = datLoader;

        private readonly List<string> _loadedBnkDataLanguages = [];
        private bool _isDatDataLoaded = false;

        public Dictionary<uint, List<HircItem>> HircsById { get; set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioListById { get; set; }
        public Dictionary<string, PackFile> PackFileByBnkName { get; set; }
        public Dictionary<uint, string> NameById { get; set; }
        public Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; set; }
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupByStateGroupByDialogueEvent { get; set; }
        public Dictionary<string, List<string>> StatesByStateGroup { get; set; }

        public void Load(List<string> languages)
        {
            var loadedData = false;

            var gameInformation = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame);
            var gameBankGeneratorVersion = gameInformation.BankGeneratorVersion;

            if (gameBankGeneratorVersion != GameBnkVersion.Unsupported)
            {
                var loadDatData = !_isDatDataLoaded;
                var loadBnkData = !languages.All(language => _loadedBnkDataLanguages.Contains(language, StringComparer.OrdinalIgnoreCase));

                if (loadDatData || loadBnkData)
                    MemoryOptimiser.LogMemory("Before loading AudioRepository");

                if (loadDatData)
                {
                    LoadDatData();
                    loadedData = true;
                }

                if (loadBnkData)
                {
                    LoadBnkData(languages);
                    loadedData = true;
                }
            }

            if (loadedData)
            {
                MemoryOptimiser.Optimise();
                MemoryOptimiser.LogMemory("After loading AudioRepository");
            }
        }

        private void LoadDatData()
        {
            var result = _datLoader.LoadDatData();
            NameById = result.NameById ?? [];
            StateGroupsByDialogueEvent = result.StateGroupsByDialogueEvent ?? [];
            QualifiedStateGroupByStateGroupByDialogueEvent = result.QualifiedStateGroupByStateGroupByDialogueEvent ?? [];
            StatesByStateGroup = result.StatesByStateGroup ?? [];

            _isDatDataLoaded = true;
        }

        private void LoadBnkData(List<string> languages)
        {
            var allLanguages = Wh3LanguageInformation.GetAllLanguages();
            var languageToFilterOut = allLanguages
                .Where(language => !languages.Contains(language))
                .ToList();
            var result = _bnkLoader.LoadBnkFiles(languageToFilterOut);
            HircsById = result.HircsById ?? [];
            DidxAudioListById = result.DidxAudioListById ?? [];
            PackFileByBnkName = result.PackFileByBnkName ?? [];

            _loadedBnkDataLanguages.AddRange(languages);
        }

        public List<T> GetHircsByType<T>() where T : class
        {
            return HircsById.Values
                .SelectMany(items => items)
                .OfType<T>()
                .ToList();
        }

        public List<HircItem> GetHircsByHircType(AkBkHircType hircType)
        {
            return HircsById.SelectMany(x => x.Value)
                .Where(hirc => hirc.HircType == hircType)
                .ToList();
        }

        public List<HircItem> GetHircs(uint id)
        {
            if (HircsById.TryGetValue(id, out var value))
                return value;
            return [];
        }

        public List<HircItem> GetHircs(uint id, string owningFileName) => GetHircs(id).Where(x => x.BnkFilePath == owningFileName).ToList();

        public string GetNameFromId(uint value, out bool found)
        {
            found = NameById.ContainsKey(value);
            if (found)
                return NameById[value];
            return value.ToString();
        }

        public string GetNameFromId(uint value) => GetNameFromId(value, out var _);

        public string GetNameFromId(uint? key)
        {
            if (key.HasValue)
                return GetNameFromId(key.Value);
            else
                throw new Exception("Cannot get name from ID");
        }

        public HashSet<uint> GetUsedVanillaHircIdsByLanguageId(uint languageId)
        {
            return HircsById
                .SelectMany(hircLookupEntry => hircLookupEntry.Value
                    .Where(hirc => hirc.LanguageId == languageId && hirc.IsCAHircItem == true)
                    .Select(_ => hircLookupEntry.Key))
                .ToHashSet();
        }

        public HashSet<uint> GetUsedVanillaSourceIdsByLanguageId(uint languageId)
        {
            return HircsById
                .SelectMany(hircLookupEntry => hircLookupEntry.Value
                    .Where(hirc => hirc.LanguageId == languageId && hirc is ICAkSound && hirc.IsCAHircItem == true)
                    .Select(hirc => ((ICAkSound)hirc).GetSourceId()))
                .ToHashSet();
        }

        public Dictionary<string, Dictionary<string, List<HircItem>>> GetVanillaDialogueEventsByBnkByLanguage()
        {
            return GetHircsByType<ICAkDialogueEvent>()
                .Select(hirc => hirc as HircItem)
                .Where(hirc => hirc.IsCAHircItem)
                .GroupBy(hirc => GetNameFromId(hirc.LanguageId))
                .ToDictionary(
                    languageGroup => languageGroup.Key,
                    languageGroup => languageGroup
                        .GroupBy(hirc => hirc.BnkFilePath)
                        .ToDictionary(bnkGroup => bnkGroup.Key, bnkGroup => bnkGroup.ToList())
                );
        }

        public Dictionary<string, Dictionary<string, List<HircItem>>> GetModdedHircsByBnkByLanguage()
        {
            return HircsById
                .SelectMany(hirc => hirc.Value)
                .Where(hirc => hirc.IsCAHircItem == false)
                .GroupBy(hirc => GetNameFromId(hirc.LanguageId))
                .ToDictionary(
                    languageGroup => languageGroup.Key,
                    languageGroup => languageGroup
                        .GroupBy(hircItem => hircItem.BnkFilePath)
                        .ToDictionary(bnkGroup => bnkGroup.Key, bnkGroup => bnkGroup.ToList())
                );
        }

        public Dictionary<string, List<HircItem>> GetModdedDialogueEventsByLanguage(List<string> moddedSoundBanks)
        {
            return GetHircsByType<ICAkDialogueEvent>()
                .Select(hirc => hirc as HircItem)
                .Where(hirc => hirc.IsCAHircItem == false && moddedSoundBanks.Contains(hirc.BnkFilePath))
                .GroupBy(hirc => GetNameFromId(hirc.LanguageId))
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        public List<string> GetModdedSoundBankFilePaths(string bnkNameSubstring)
        {
            return HircsById
                .SelectMany(hircDictionaryEntry => hircDictionaryEntry.Value) 
                .Where(hirc => hirc.IsCAHircItem == false && hirc.BnkFilePath.Contains(bnkNameSubstring))
                .Select(hirc => hirc.BnkFilePath ) 
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(bnkFilePath => bnkFilePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void Clear()
        {
            MemoryOptimiser.LogMemory("Before clearing AudioRepository");

            if (HircsById != null)
            {
                foreach (var list in HircsById.Values)
                {
                    list?.Clear();
                    list?.TrimExcess();
                }
                HircsById.Clear();
                HircsById = null;
            }

            if (DidxAudioListById != null)
            {
                foreach (var list in DidxAudioListById.Values)
                {
                    list?.Clear();
                    list?.TrimExcess();
                }
                DidxAudioListById.Clear();
                DidxAudioListById = null;
            }

            _loadedBnkDataLanguages?.Clear();
            _isDatDataLoaded = false;
            PackFileByBnkName?.Clear();
            PackFileByBnkName = null;
            NameById?.Clear();
            NameById = null;
            StateGroupsByDialogueEvent?.Clear();
            StateGroupsByDialogueEvent = null;
            QualifiedStateGroupByStateGroupByDialogueEvent?.Clear();
            QualifiedStateGroupByStateGroupByDialogueEvent = null;
            StatesByStateGroup?.Clear();
            StatesByStateGroup = null;

            MemoryOptimiser.Optimise();
            MemoryOptimiser.LogMemory("After clearing AudioRepository");
        }

        public void Dispose() => Clear();
    }
}
