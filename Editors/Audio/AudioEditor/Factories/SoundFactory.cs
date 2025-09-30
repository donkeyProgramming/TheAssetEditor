using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioProjectCompiler;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface ISoundFactory
    {
        Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, AudioSettings audioSettings, uint overrideBusId = 0, uint directParentId = 0);
        Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, uint directParentId);
    }

    public class SoundFactory : ISoundFactory
    {
        public Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, AudioSettings audioSettings, uint overrideBusId = 0, uint directParentId = 0)
        {
            var fileName = audioFile.FileName;
            var filePath = audioFile.FilePath;
            var soundIds = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
            var sourceId = IdGenerator.GenerateSourceId(usedHircIds, filePath);
            var soundSettings = AudioSettings.CreateSoundSettings(audioSettings);
            return Sound.Create(soundIds.Guid, soundIds.Id, overrideBusId, directParentId, sourceId, fileName, filePath, soundSettings);
        }

        public Sound Create(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, AudioFile audioFile, uint directParentId)
        {
            var fileName = audioFile.FileName;
            var filePath = audioFile.FilePath;
            var soundIds = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
            var sourceId = IdGenerator.GenerateSourceId(usedHircIds, filePath);
            return Sound.Create(soundIds.Guid, soundIds.Id, directParentId, sourceId, fileName, filePath);
        }
    }
}
