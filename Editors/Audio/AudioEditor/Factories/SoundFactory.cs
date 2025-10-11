using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioProjectCompiler;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface ISoundFactory
    {
        Sound Create(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            AudioFile audioFile,
            AudioSettings audioSettings,
            string language,
            uint overrideBusId = 0,
            uint directParentId = 0);
        Sound Create(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            AudioFile audioFile,
            uint directParentId,
            int playlistOrder,
            string language);
    }

    public class SoundFactory : ISoundFactory
    {
        public Sound Create(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            AudioFile audioFile,
            AudioSettings audioSettings,
            string language,
            uint overrideBusId = 0,
            uint directParentId = 0)
        {
            var soundIds = IdGenerator.GenerateIds(usedHircIds);
            var soundSettings = AudioSettings.CreateSoundSettings(audioSettings);
            return Sound.Create(soundIds.Guid, soundIds.Id, overrideBusId, directParentId, audioFile.Id, language, soundSettings);
        }

        public Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, uint directParentId, int playlistOrder, string language)
        {
            var soundIds = IdGenerator.GenerateIds(usedHircIds);
            return Sound.Create(soundIds.Guid, soundIds.Id, directParentId, playlistOrder, audioFile.Id, language);
        }
    }
}
