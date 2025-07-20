using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface ISoundFactory
    {
        Sound Create(AudioFile audioFile, AudioSettings audioSettings);
        Sound Create(AudioFile audioFiles);
    }

    public class SoundFactory : ISoundFactory
    {
        public Sound Create(AudioFile audioFile, AudioSettings audioSettings)
        {
            var fileName = audioFile.FileName;
            var filePath = audioFile.FilePath;
            var soundSettings = AudioSettings.CreateSoundSettings(audioSettings);
            return Sound.Create(fileName, filePath, soundSettings);
        }

        public Sound Create(AudioFile audioFile)
        {
            var fileName = audioFile.FileName;
            var filePath = audioFile.FilePath;
            return Sound.Create(fileName, filePath);
        }
    }
}
