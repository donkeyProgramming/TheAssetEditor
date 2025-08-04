using System.Linq;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.Models;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IAudioProjectCompilerService
    {
        void Compile(AudioProject audioProject, string audioProjectFileName, string audioProjectFilePath);
    }

    public class AudioProjectCompilerService(
        IAudioProjectFileService audioProjectFileService,
        CompilerDataProcessor compilerDataProcessor,
        SoundBankGenerator soundBankGenerator,
        WemGenerator wemGenerator,
        DatGenerator datGenerator) : IAudioProjectCompilerService
    {
        private readonly IAudioProjectFileService _audioProjectFileService = audioProjectFileService;
        private readonly CompilerDataProcessor _compilerDataProcessor = compilerDataProcessor;
        private readonly SoundBankGenerator _soundBankGenerator = soundBankGenerator;
        private readonly WemGenerator _wemGenerator = wemGenerator;
        private readonly DatGenerator _datGenerator = datGenerator;

        public void Compile(AudioProject audioProject, string audioProjectFileName, string audioProjectFilePath)
        {
            if (audioProject.SoundBanks == null)
                return;

            var audioProjectFileNameWithoutSpaces = audioProjectFileName.Replace(" ", "_");
            _compilerDataProcessor.SetSoundBankData(audioProject, audioProjectFileNameWithoutSpaces);

            // We set the data from the bottom up, so Sounds, then Actions, then Events to ensure that IDs are generated before
            // they're referenced e.g. Sounds / Random Sequence Container IDs are used in Actions, and Action IDs are used in Events
            _compilerDataProcessor.SetInitialSourceData(audioProject);

            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                _compilerDataProcessor.CreateStopActionEvents(audioProject);
                _compilerDataProcessor.SetActionData(audioProject);
                _compilerDataProcessor.SetActionEventData(audioProject);
            }

            if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
            {
                _compilerDataProcessor.SetStatesData(audioProject);
                _compilerDataProcessor.SetDialogueEventData(audioProject);
            }

            _wemGenerator.GenerateWems(audioProject);

            _compilerDataProcessor.SetRemainingSourceData(audioProject);

            _wemGenerator.SaveWemsToPack(audioProject);

            _soundBankGenerator.GenerateSoundBanks(audioProject);

            _datGenerator.GenerateDatFiles(audioProject, audioProjectFileNameWithoutSpaces);

            var compiledAudioProjectFileName = audioProjectFileName.Replace(".aproj", "_compiled.json");
            var compiledAudioProjectFilePath = audioProjectFilePath.Replace(".aproj", "_compiled.json");
            _audioProjectFileService.Save(audioProject, compiledAudioProjectFileName, compiledAudioProjectFilePath);
        }
    }
}
