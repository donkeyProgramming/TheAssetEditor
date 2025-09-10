using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Core.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Storage
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
        Dictionary<string, Dictionary<uint, string>> StatesByStateGroupByStateId { get; }

        List<T> GetHircItemsByType<T>() where T : class;
        List<HircItem> GetHircObject(uint id);
        List<HircItem> GetHircObject(uint id, string owningFileName);
        string GetNameFromId(uint value);
        string GetNameFromId(uint value, out bool found);
        string GetNameFromId(uint? key);
        Dictionary<string, HashSet<uint>> GetUsedHircIdsByBnkFilePathByLanguageId(uint languageId);
        Dictionary<string, HashSet<uint>> GetUsedSourceIdsByBnkFilePathByLanguageId(uint languageId);
    }

    public class AudioRepository : IAudioRepository, IDisposable
    {
        private readonly BnkLoader _bnkLoader;
        private readonly DatLoader _datLoader;

        public Dictionary<uint, List<HircItem>> HircsById { get; set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioListById { get; set; }
        public Dictionary<string, PackFile> PackFileByBnkName { get; set; }
        public Dictionary<uint, string> NameById { get; set; }
        public Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; set; }
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupByStateGroupByDialogueEvent { get; set; }
        public Dictionary<string, List<string>> StatesByStateGroup { get; set; }
        public Dictionary<string, Dictionary<uint, string>> StatesByStateGroupByStateId { get; set; }

        public AudioRepository(ApplicationSettingsService applicationSettingsService, BnkLoader bnkLoader, DatLoader datLoader)
        {
            _bnkLoader = bnkLoader;
            _datLoader = datLoader;

            MemoryOptimiser.LogMemory("Before loading AudioRepository");

            var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
            var gameBankGeneratorVersion = gameInformation.BankGeneratorVersion;
            if (gameBankGeneratorVersion != GameBnkVersion.Unsupported)
            {
                LoadDatData();
                LoadBnkData();
            }

            MemoryOptimiser.Optimise();
            MemoryOptimiser.LogMemory("After loading AudioRepository");
        }

        private void LoadDatData()
        {
            var loadResult = _datLoader.LoadDatData();
            NameById = loadResult.NameById ?? [];
            StateGroupsByDialogueEvent = loadResult.StateGroupsByDialogueEvent ?? [];
            QualifiedStateGroupByStateGroupByDialogueEvent = loadResult.QualifiedStateGroupByStateGroupByDialogueEvent ?? [];
            StatesByStateGroup = loadResult.StatesByStateGroup ?? [];
            StatesByStateGroupByStateId = loadResult.StatesByStateGroupByStateId ?? [];
        }

        private void LoadBnkData()
        {
            var loadResult = _bnkLoader.LoadBnkFiles();
            HircsById = loadResult.HircsById ?? [];
            DidxAudioListById = loadResult.DidxAudioListById ?? [];
            PackFileByBnkName = loadResult.PackFileByBnkName ?? [];
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (HircsById.ContainsKey(id))
                return HircsById[id];
            return [];
        }

        public List<HircItem> GetHircObject(uint id, string owningFileName)
        {
            var hircItems = GetHircObject(id).Where(x => x.BnkFilePath == owningFileName).ToList();
            return hircItems;
        }

        public string GetNameFromId(uint value, out bool found)
        {
            found = NameById.ContainsKey(value);
            if (found)
                return NameById[value];
            return value.ToString();
        }

        public List<T> GetHircItemsByType<T>() where T : class
        {
            return HircsById.Values
                .SelectMany(items => items)
                .OfType<T>()
                .ToList();
        }

        public string GetNameFromId(uint value) => GetNameFromId(value, out var _);

        public string GetNameFromId(uint? key)
        {
            if (key.HasValue)
                return GetNameFromId(key.Value);
            else
                throw new Exception("Cannot get name from ID");
        }

        public Dictionary<string, HashSet<uint>> GetUsedHircIdsByBnkFilePathByLanguageId(uint languageId)
        {
            return HircsById
                .Where(hircLookupEntry => hircLookupEntry.Value.Any(hircItem => hircItem.LanguageId == languageId))
                .SelectMany(hircLookupEntry => hircLookupEntry.Value
                    .Where(hircItem => hircItem.LanguageId == languageId)
                    .Select(hircItem => new { hircItem.BnkFilePath, Id = hircLookupEntry.Key }))
                .GroupBy(hircItem => hircItem.BnkFilePath)
                .ToDictionary(group => group.Key, group => group.Select(x => x.Id).ToHashSet()
                );
        }

        public Dictionary<string, HashSet<uint>> GetUsedSourceIdsByBnkFilePathByLanguageId(uint languageId)
        {
            return HircsById
                .SelectMany(hircLookupEntry => hircLookupEntry.Value
                    .Where(hircItem => hircItem.LanguageId == languageId && hircItem is ICAkSound)
                    .Select(hircItem => new { hircItem.BnkFilePath, SourceId = ((ICAkSound)hircItem).GetSourceId() }))
                .GroupBy(hircItem => hircItem.BnkFilePath)
                .ToDictionary(group => group.Key, group => group.Select(x => x.SourceId).ToHashSet());
        }

        public void Dispose()
        {
            MemoryOptimiser.LogMemory("Before disposing of AudioRepository");

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

            PackFileByBnkName?.Clear(); PackFileByBnkName = null;
            NameById?.Clear(); NameById = null;
            StateGroupsByDialogueEvent?.Clear(); StateGroupsByDialogueEvent = null;
            QualifiedStateGroupByStateGroupByDialogueEvent?.Clear(); QualifiedStateGroupByStateGroupByDialogueEvent = null;
            StatesByStateGroup?.Clear(); StatesByStateGroup = null;
            StatesByStateGroupByStateId?.Clear(); StatesByStateGroupByStateId = null;

            MemoryOptimiser.Optimise();
            MemoryOptimiser.LogMemory("After disposing of AudioRepository");
        }
    }
}
