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
        public Dictionary<uint, Dictionary<uint, List<HircItem>>> HircLookupByLanguageIDByID { get; internal set; }
        public Dictionary<uint, Dictionary<uint, List<ICAkSound>>> SoundHircLookupByLanguageIDBySourceID { get; internal set; }
        public Dictionary<uint, Dictionary<uint, List<DidxAudio>>> DidxAudioLookupByLanguageIDByID { get; internal set; }
        public Dictionary<uint, List<HircItem>> HircLookupByID { get; internal set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioLookupByID { get; internal set; }
        public Dictionary<string, PackFile> BnkPackFileLookupByName { get; internal set; }
        public Dictionary<uint, string> NameLookupByID { get; internal set; }
        public Dictionary<string, List<string>> StateGroupsLookupByDialogueEvent { get; set; }
        public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupLookupByStateGroupByDialogueEvent { get; set; }
        public Dictionary<string, List<string>> StatesLookupByStateGroup { get; set; }
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
            audioData.NameLookupByID = loadResult.NameLookupByID;
            audioData.StateGroupsLookupByDialogueEvent = loadResult.StateGroupsLookupByDialogueEvent;
            audioData.QualifiedStateGroupLookupByStateGroupByDialogueEvent = loadResult.QualifiedStateGroupLookupByStateGroupByDialogueEvent;
            audioData.StatesLookupByStateGroup = loadResult.StatesLookupByStateGroup;
        }

        public void LoadBnkData(AudioData audioData)
        {
            var loadResult = _bnkLoader.LoadBnkFiles();
            audioData.HircLookupByLanguageIDByID = loadResult.HircLookupByLanguageIDByID;
            audioData.SoundHircLookupByLanguageIDBySourceID = loadResult.SoundHircLookupByLanguageIDBySourceID;
            audioData.DidxAudioLookupByLanguageIDByID = loadResult.DidxAudioLookupByLanguageIDByID;
            audioData.HircLookupByID = loadResult.HircLookupByID;
            audioData.DidxAudioLookupByID = loadResult.DidxAudioLookupByID;
            audioData.BnkPackFileLookupByName = loadResult.BnkPackFileLookupByName;
        }
    }
}
