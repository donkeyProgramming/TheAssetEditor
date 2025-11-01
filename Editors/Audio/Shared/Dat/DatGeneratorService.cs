using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Dat;

namespace Editors.Audio.Shared.Dat
{
    public interface IDatGeneratorService
    {
        void GenerateEventDatFile(string audioProjectFileNameWithoutExtension, List<ActionEvent> actionEvents = null, List<StateGroup> stateGroups = null);
    }

    public class DatGeneratorService(IFileSaveService fileSaveService) : IDatGeneratorService
    {
        private readonly IFileSaveService _fileSaveService = fileSaveService;

        public void GenerateEventDatFile(string audioProjectFileNameWithoutExtension, List<ActionEvent> actionEvents = null, List<StateGroup> stateGroups = null)
        {
            var datFile = new SoundDatFile();

            if (actionEvents != null && actionEvents.Count > 0)
            {
                foreach (var actionEvent in actionEvents)
                    datFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = actionEvent.Name, Value = 400 });
            }

            if (stateGroups != null && stateGroups.Count > 0)
            {

                foreach (var stateGroup in stateGroups)
                {
                    var states = new List<string>();
                    foreach (var state in stateGroup.States)
                        states.Add(state.Name);

                    datFile.StateGroupsWithStates1.Add(new SoundDatFile.DatStateGroupsWithStates() { StateGroup = stateGroup.Name, States = states });
                }
            }

            var datFileName = $"event_data__{audioProjectFileNameWithoutExtension}.dat";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(datFile, datFileName, datFilePath);
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
