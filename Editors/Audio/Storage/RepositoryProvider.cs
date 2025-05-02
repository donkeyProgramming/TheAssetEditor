using System.Collections.Generic;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Storage
{
    // TODO: maybe get rid of provider
    public interface RepositoryProvider
    {
        void LoadBnkData(AudioData audioData);
        void LoadDatData(AudioData audioData);
    }

    public class AudioData
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
    }

    public class CreateRepositoryFromAllPackFiles : RepositoryProvider
    {
        private readonly BnkLoader _bnkLoader;
        private readonly DatLoader _datLoader;

        public CreateRepositoryFromAllPackFiles(BnkLoader bnkLoader, DatLoader datLoader)
        {
            _bnkLoader = bnkLoader;
            _datLoader = datLoader;
        }

        public void LoadDatData(AudioData audioData)
        {
            var loadResult = _datLoader.LoadDatData();
            audioData.NameLookupById = loadResult.NameLookupById;
            audioData.StateGroupsLookupByDialogueEvent = loadResult.StateGroupsLookupByDialogueEvent;
            audioData.QualifiedStateGroupLookupByStateGroupByDialogueEvent = loadResult.QualifiedStateGroupLookupByStateGroupByDialogueEvent;
            audioData.StatesLookupByStateGroup = loadResult.StatesLookupByStateGroup;
            audioData.StatesLookupByStateGroupByStateId = loadResult.StatesLookupByStateGroupByStateId;
        }

        public void LoadBnkData(AudioData audioData)
        {
            var loadResult = _bnkLoader.LoadBnkFiles();
            audioData.HircLookupByLanguageIdById = loadResult.HircLookupByLanguageIdById;
            audioData.SoundHircLookupByLanguageIdBySourceId = loadResult.SoundHircLookupByLanguageIdBySourceId;
            audioData.DidxAudioLookupByLanguageIdById = loadResult.DidxAudioLookupByLanguageIdById;
            audioData.HircLookupById = loadResult.HircLookupById;
            audioData.DidxAudioLookupById = loadResult.DidxAudioLookupById;
            audioData.BnkPackFileLookupByName = loadResult.BnkPackFileLookupByName;
        }
    }
}
