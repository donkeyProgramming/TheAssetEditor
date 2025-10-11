using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioProjectCompiler;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface ISoundFactory
    {
        Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, AudioSettings audioSettings, uint overrideBusId = 0, uint directParentId = 0);
        Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, uint directParentId, int playlistOrder);
    }

    public class SoundFactory : ISoundFactory
    {
        public Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, AudioSettings audioSettings, uint overrideBusId = 0, uint directParentId = 0)
        {
            var soundIds = IdGenerator.GenerateIds(usedHircIds);
            var soundSettings = AudioSettings.CreateSoundSettings(audioSettings);
            return Sound.Create(soundIds.Guid, soundIds.Id, overrideBusId, directParentId, audioFile.Id, soundSettings);
        }

        public Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, uint directParentId, int playlistOrder)
        {
            var soundIds = IdGenerator.GenerateIds(usedHircIds);
            return Sound.Create(soundIds.Guid, soundIds.Id, directParentId, playlistOrder, audioFile.Id);
        }
    }
}
