using Audio.FileFormats.WWise;
using Audio.Utility;
using CommonControls.Editors.AudioEditor;
using CommonControls.Services;
using System.Collections.Generic;

namespace Audio.Storage
{
    public interface RepositoryProvider
    {
        AudioData Load();
    }

    public class AudioData
    {
        public Dictionary<uint, string> NameLookup { get; set; }
        public Dictionary<uint, List<HircItem>> HircObjects { get; set; }
    }

    public class CreateRepositoryFromAllPackFiles : RepositoryProvider
    {
        WwiseDataLoader _wwiseDataLoader;
        PackFileService _pfs;

        public CreateRepositoryFromAllPackFiles(WwiseDataLoader wwiseDataLoader, PackFileService pfs)
        {
            _wwiseDataLoader = wwiseDataLoader;
            _pfs = pfs;
        }

        public AudioData Load()
        {
            WwiseNameLookupBuilder load = new WwiseNameLookupBuilder();

            var bnkList = _wwiseDataLoader.LoadBnkFiles(_pfs);
            //var globalDb = _wwiseDataLoader.BuildMasterSoundDatabase(bnkList);

            return new AudioData()
            {
                NameLookup = load.BuildNameHelper(_pfs),
                HircObjects = bnkList.HircList
            };
        }
    }

}
