using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.Shared.AudioProject.Factories
{
    public interface ISoundFactory
    {
        Sound CreateTargetSound(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            AudioFile audioFile,
            HircSettings hircSettings,
            string language,
            uint overrideBusId = 0,
            uint directParentId = 0);
        Sound CreateContainerSound(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            AudioFile audioFile,
            uint directParentId,
            int playlistOrder,
            string language);
    }

    public class SoundFactory : ISoundFactory
    {
        public Sound CreateTargetSound(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            AudioFile audioFile,
            HircSettings hircSettings,
            string language,
            uint overrideBusId = 0,
            uint directParentId = 0)
        {
            var soundIds = IdGenerator.GenerateIds(usedHircIds);
            var soundSettings = HircSettings.CreateSoundSettings(hircSettings);
            return Sound.CreateTargetSound(soundIds.Guid, soundIds.Id, overrideBusId, directParentId, audioFile.Id, language, soundSettings);
        }

        public Sound CreateContainerSound(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, uint directParentId, int playlistOrder, string language)
        {
            var soundIds = IdGenerator.GenerateIds(usedHircIds);
            return Sound.CreateContainerSound(soundIds.Guid, soundIds.Id, directParentId, playlistOrder, audioFile.Id, language);
        }
    }
}
