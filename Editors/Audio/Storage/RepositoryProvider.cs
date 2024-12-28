using System.Collections.Generic;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Didx;

namespace Editors.Audio.Storage
{
    public interface RepositoryProvider
    {
        void LoadBnkData(AudioData audioData);
        void LoadDatData(AudioData audioData);
    }

    public class AudioData
    {
        public Dictionary<uint, List<HircItem>> HircObjects { get; internal set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioObject { get; internal set; }
        public Dictionary<string, PackFile> PackFileMap { get; internal set; } = [];
        public Dictionary<uint, string> NameLookUpTable { get; internal set; }
        public Dictionary<string, List<string>> DialogueEventsWithStateGroups { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiersAndStateGroups { get; set; } = [];
        public Dictionary<string, List<string>> StateGroupsWithStates { get; set; } = [];
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
            audioData.NameLookUpTable = loadResult.NameLookUpTable;
            audioData.DialogueEventsWithStateGroups = loadResult.DialogueEventsWithStateGroups;
            audioData.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups = loadResult.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups;
            audioData.StateGroupsWithStates = loadResult.StateGroupsWithStates;
        }

        public void LoadBnkData(AudioData audioData)
        {
            var loadResult = _bnkLoader.LoadBnkFiles();
            audioData.HircObjects = loadResult.HircList;
            audioData.DidxAudioObject = loadResult.DidxAudioList;
            audioData.PackFileMap = loadResult.PackFileMap;
        }
    }
}
