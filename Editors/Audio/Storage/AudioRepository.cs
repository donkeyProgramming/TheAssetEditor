using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Storage
{
    public interface IAudioRepository
    {
        Dictionary<uint, List<HircItem>> HircLookupById { get; }
        Dictionary<uint, List<DidxAudio>> DidxAudioLookupById { get; }
        Dictionary<string, PackFile> PackFileLookupByBnkName { get; }
        Dictionary<uint, string> NameLookupById { get; }
        Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; }
        Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; }
        Dictionary<string, List<string>> StatesLookupByStateGroup { get; }
        Dictionary<string, Dictionary<uint, string>> StatesLookupByStateGroupByStateId { get; }

        List<T> GetHircItemsByType<T>() where T : class;
        List<HircItem> GetHircObject(uint id);
        List<HircItem> GetHircObject(uint id, string owningFileName);
        string GetNameFromId(uint value);
        string GetNameFromId(uint value, out bool found);
        string GetNameFromId(uint? key);
        HashSet<uint> GetUsedHircIdsByLanguageId(uint languageId);
        HashSet<uint> GetUsedSourceIdsByLanguageId(uint languageId);
    }

    public class AudioRepository : IAudioRepository
    {
        private readonly BnkLoader _bnkLoader;
        private readonly DatLoader _datLoader;

        public Dictionary<uint, List<HircItem>> HircLookupById { get; set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioLookupById { get; set; }
        public Dictionary<string, PackFile> PackFileLookupByBnkName { get; set; }
        public Dictionary<uint, string> NameLookupById { get; set; }
        public Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; set; }
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; set; }
        public Dictionary<string, List<string>> StatesLookupByStateGroup { get; set; }
        public Dictionary<string, Dictionary<uint, string>> StatesLookupByStateGroupByStateId { get; set; }

        public AudioRepository(ApplicationSettingsService applicationSettingsService, BnkLoader bnkLoader, DatLoader datLoader)
        {
            _bnkLoader = bnkLoader;
            _datLoader = datLoader;

            var gameInformation = GameInformationDatabase.GetGameById(applicationSettingsService.CurrentSettings.CurrentGame);
            var gameBankGeneratorVersion = gameInformation.BankGeneratorVersion;
            if (gameBankGeneratorVersion != GameBnkVersion.Unsupported)
            {
                LoadDatData();
                LoadBnkData();
            }

            // Run garbage collection to free up memory no longer in use
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        private void LoadDatData()
        {
            var loadResult = _datLoader.LoadDatData();
            NameLookupById = loadResult.NameLookupById ?? [];
            StateGroupsLookupByDialogueEvent = loadResult.StateGroupsLookupByDialogueEvent ?? [];
            QualifiedStateGroupLookupByStateGroupByDialogueEvent = loadResult.QualifiedStateGroupLookupByStateGroupByDialogueEvent ?? [];
            StatesLookupByStateGroup = loadResult.StatesLookupByStateGroup ?? [];
            StatesLookupByStateGroupByStateId = loadResult.StatesLookupByStateGroupByStateId ?? [];
        }

        private void LoadBnkData()
        {
            var loadResult = _bnkLoader.LoadBnkFiles();
            HircLookupById = loadResult.HircLookupById ?? [];
            DidxAudioLookupById = loadResult.DidxAudioLookupById ?? [];
            PackFileLookupByBnkName = loadResult.BnkPackFileLookupByName ?? [];
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

        public string GetNameFromId(uint? key)
        {
            if (key.HasValue)
                return GetNameFromId(key.Value);
            else
                throw new Exception("Cannot get name from ID");
        }

        public HashSet<uint> GetUsedHircIdsByLanguageId(uint languageId)
        {
            return HircLookupById
                .Where(hircLookupEntry => hircLookupEntry.Value.Any(hircItem => hircItem.LanguageId == languageId))
                .Select(hircLookupEntry => hircLookupEntry.Key)
                .ToHashSet();
        }

        public HashSet<uint> GetUsedSourceIdsByLanguageId(uint languageId)
        {
            return HircLookupById
                .SelectMany(hircLookupEntry => hircLookupEntry.Value)
                .Where(hircItem => hircItem.LanguageId == languageId)
                .OfType<ICAkSound>()
                .Select(soundHircItem => soundHircItem.GetSourceId())
                .ToHashSet();
        }
    }
}
