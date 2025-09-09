using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IDatGeneratorService
    {
        void GenerateEventDatFile(AudioProject audioProject, string audioProjectNameFileWithoutExtension);
        void GenerateStatesDatFile(AudioProject audioProject, string audioProjectNameFileWithoutExtension);
    }

    public class DatGeneratorService (IFileSaveService fileSaveService) : IDatGeneratorService
    {
        private readonly IFileSaveService _fileSaveService = fileSaveService;

        public void GenerateEventDatFile(AudioProject audioProject, string audioProjectNameFileWithoutExtension)
        {
            var soundDatFile = new SoundDatFile();

            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var actionEvent in soundBank.ActionEvents)
                {
                    // TODO: Check what other types of Action Event need a dat file
                    if (actionEvent.ActionEventType != Wh3ActionEventType.Movies)
                        continue;

                    soundDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = actionEvent.Name, Value = 400 });
                }
            }

            var datFileName = $"event_data__{audioProjectNameFileWithoutExtension}.dat";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(soundDatFile, datFileName, datFilePath);
        }

        public void GenerateStatesDatFile(AudioProject audioProject, string audioProjectNameFileWithoutExtension)
        {
            var stateDatFile = new SoundDatFile();

            foreach (var stateGroup in audioProject.StateGroups)
            {
                foreach (var state in stateGroup.States)
                    stateDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = state.Name, Value = 400 });
            }

            var datFileName = $"states_data__{audioProjectNameFileWithoutExtension}.dat";
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
