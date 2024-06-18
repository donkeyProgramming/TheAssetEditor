using System.Collections.Generic;
using Shared.GameFormats.Dat;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Didx;

namespace Editors.Audio.Storage
{
    public interface RepositoryProvider
    {
        AudioData LoadBnkAndDatData();
    }

    public class AudioData
    {
        public Dictionary<uint, string> NameLookUpTable { get; internal set; }
        public Dictionary<uint, List<HircItem>> HircObjects { get; internal set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioObject { get; internal set; }
        public List<SoundDatFile.DatDialogueEventsWithStateGroups> DialogueEventsWithStateGroups { get; internal set; }
        public List<SoundDatFile.DatStateGroupsWithStates> StateGroupsWithStates { get; internal set; }
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

        public AudioData LoadBnkAndDatData()
        {
            var (nameLookUp, dialogueEventsWithStateGroups, stateGroupsWithStates) = _datLoader.LoadDatData();
            var loadResult = _bnkLoader.LoadBnkFiles();

            return new AudioData()
            {
                NameLookUpTable = nameLookUp,
                DialogueEventsWithStateGroups = dialogueEventsWithStateGroups,
                StateGroupsWithStates = stateGroupsWithStates,
                HircObjects = loadResult.HircList,
                DidxAudioObject = loadResult.DidxAudioList
            };
        }
    }
}
