using System;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.HircExploration;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Utilities
{
    public interface IMovieAudioResolver
    {
        PackFile ResolveMovieWem(string caVp8PackFilePath);
    }

    public class MovieAudioResolver(IAudioRepository audioRepository) : IMovieAudioResolver
    {
        private readonly IAudioRepository _audioRepository = audioRepository;

        public PackFile ResolveMovieWem(string caVp8PackFilePath)
        {
            var actionEventName = Wh3ActionEventInformation.GetMovieActionEventName(caVp8PackFilePath);
            var actionEventId = WwiseHash.Compute(actionEventName);

            var actionEventHircs = _audioRepository.GetHircs(actionEventId);
            if (actionEventHircs.Count == 0)
                throw new Exception($"Cannot find Action Event: {actionEventName}.");

            var hircTreeChildrenParser = new HircTreeChildrenParser(_audioRepository);
            var nodes = hircTreeChildrenParser.BuildHierarchyAsFlatList(actionEventHircs.First());

            var sound = nodes.FirstOrDefault(node => node.Hirc is ICAkSound)?.Hirc as ICAkSound;
            if (sound == null)
                throw new Exception($"Cannot find a Sound for Action Event: {actionEventName}.");

            var sourceId = sound.GetSourceId();
            var wemPackFile = _audioRepository.FindWem(sourceId.ToString());
            if (wemPackFile == null)
                throw new Exception($"Cannot find {sourceId}.wem");

            return wemPackFile;
        }
    }
}