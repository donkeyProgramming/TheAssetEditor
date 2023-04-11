using Audio.FileFormats.WWise;
using CommonControls.Common;
using System.Collections.Generic;

namespace Audio.Storage
{
    public interface RepositoryProvider
    {
        AudioData Load();
    }

    public class AudioData
    {
        public Dictionary<uint, string> NameLookUpTable { get; set; }
        public Dictionary<uint, List<HircItem>> HircObjects { get; set; }
    }

    public class CreateRepositoryFromAllPackFiles : RepositoryProvider
    {
        private readonly WWiseBnkLoader _wwiseDataLoader;
        private readonly WWiseNameLoader _wwiseNameLoader;

        public CreateRepositoryFromAllPackFiles(WWiseBnkLoader wwiseDataLoader, WWiseNameLoader wwiseNameLoader)
        {
            _wwiseDataLoader = wwiseDataLoader;
            _wwiseNameLoader = wwiseNameLoader;
        }

        public AudioData Load()
        {
            using var _ = new WaitCursor();
    
            var nameList = _wwiseNameLoader.BuildNameHelper();
            var bnkList = _wwiseDataLoader.LoadBnkFiles();

            return new AudioData()
            {
                NameLookUpTable = nameList,
                HircObjects = bnkList
            };
        }
    }

}
