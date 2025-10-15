using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;

namespace Editors.Audio.Shared.Dat
{
    public interface IDatGeneratorService
    {
        void GenerateEventDatFile(List<ActionEvent> actionEvents, string audioProjectFileNameWithoutExtension);
        void GenerateStatesDatFile(List<StateGroup> stateGroups, string audioProjectFileNameWithoutExtension);
    }

    public class DatGeneratorService(IFileSaveService fileSaveService) : IDatGeneratorService
    {
        private readonly IFileSaveService _fileSaveService = fileSaveService;

        public void GenerateEventDatFile(List<ActionEvent> actionEvents, string audioProjectFileNameWithoutExtension)
        {
            var soundDatFile = new SoundDatFile();

            foreach (var actionEvent in actionEvents)
                soundDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = actionEvent.Name, Value = 400 });

            var datFileName = $"event_data__{audioProjectFileNameWithoutExtension}.dat";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(soundDatFile, datFileName, datFilePath);
        }

        public void GenerateStatesDatFile(List<StateGroup> stateGroups, string audioProjectFileNameWithoutExtension)
        {
            var stateDatFile = new SoundDatFile();

            foreach (var stateGroup in stateGroups)
            {
                foreach (var state in stateGroup.States)
                    stateDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = state.Name, Value = 400 });
            }

            var datFileName = $"states_data__{audioProjectFileNameWithoutExtension}.dat";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(stateDatFile, datFileName, datFilePath);
        }

        private void SaveDatFileToPack(SoundDatFile datFile, string datFileName, string datFilePath)
        {
            var bytes = DatFileParser.WriteData(datFile);
            var packFile = new PackFile(datFileName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            _fileSaveService.Save(datFilePath, packFile.DataSource.ReadData(), false);
        }
    }
}
