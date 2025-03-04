using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;

namespace Editors.Audio.AudioProjectCompiler
{
    public class DatGenerator
    {
        private readonly IFileSaveService _fileSaveService;

        public DatGenerator(IFileSaveService fileSaveService)
        {
            _fileSaveService = fileSaveService;
        }

        public void GenerateDatFiles(AudioProject audioProject, string audioProjectFileName)
        {
            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                foreach (var soundBank in audioProject.SoundBanks)
                    GenerateEventDatFile(soundBank);
            }

            if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
                GenerateStatesDatFile(audioProject, audioProjectFileName);
        }

        private void GenerateEventDatFile(SoundBank soundBank)
        {
            // TODO: Need to impelement something so it only makes dat files for the soundbanks that need it, so far I think that's at least movies.
            var soundDatFile = new SoundDatFile();

            foreach (var actionEvent in soundBank.ActionEvents)
                soundDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = actionEvent.Name, Value = 400 });

            var datFileName = $"event_data__{soundBank.SoundBankFileName.Replace(".bnk", ".dat")}";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(soundDatFile, datFileName, datFilePath);
        }

        private void GenerateStatesDatFile(AudioProject audioProject, string audioProjectFileName)
        {
            var stateDatFile = new SoundDatFile();

            foreach (var stateGroup in audioProject.StateGroups)
            {
                foreach (var state in stateGroup.States)
                    stateDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = state.Name, Value = 400 });
            }

            var datFileName = $"states_data__{audioProjectFileName}.dat";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(stateDatFile, datFileName, datFilePath);
        }

        private void SaveDatFileToPack(SoundDatFile datFile, string datFileName, string datFilePath)
        {
            var bytes = DatFileParser.WriteData(datFile);
            var packFile = new PackFile(datFileName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            _fileSaveService.Save(datFilePath, packFile.DataSource.ReadData(), true);
        }
    }
}
