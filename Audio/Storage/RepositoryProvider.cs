using System.Collections.Generic;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Didx;

namespace Audio.Storage
{
    public interface RepositoryProvider
    {
        AudioData LoadWwiseBnkAndDatData();
        AudioData LoadWwiseDatData();

    }

    public class AudioData
    {
        public Dictionary<uint, string> NameLookUpTable { get; internal set; }
        public Dictionary<uint, List<HircItem>> HircObjects { get; internal set; }
        public Dictionary<uint, List<DidxAudio>> DidxAudioObject { get; internal set; }
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

        public AudioData LoadWwiseBnkAndDatData()
        {
            var nameList = _wwiseNameLoader.BuildNameHelper();
            var loadResult = _wwiseDataLoader.LoadBnkFiles();

            return new AudioData()
            {
                NameLookUpTable = nameList,
                HircObjects = loadResult.HircList,
                DidxAudioObject = loadResult.DidxAudioList
            };
        }

        public AudioData LoadWwiseDatData()
        {
            var nameList = _wwiseNameLoader.BuildNameHelper();

            return new AudioData()
            {
                NameLookUpTable = nameList
            };
        }
    }
}
