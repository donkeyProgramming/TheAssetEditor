using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.Dat;

namespace Editors.Audio.Storage
{
    public class DatLoader
    {
        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _applicationSettingsService;

        private Dictionary<uint, string> _nameLookUp { get; set; } = new Dictionary<uint, string>();

        public DatLoader(IPackFileService pfs, ApplicationSettingsService applicationSettingsService)
        {
            _pfs = pfs;
            _applicationSettingsService = applicationSettingsService;
        }

        public (Dictionary<uint, string> nameLookUp, List<SoundDatFile.DatDialogueEventsWithStateGroups> dialogueEventsWithStateGroups, List<SoundDatFile.DatStateGroupsWithStates> stateGroupsWithStates) LoadDatData()
        {
            var wh3Db = LoadDatFiles(_pfs, out var _);
            var nameLookUp = BuildNameHelper(wh3Db);
            var dialogueEventsWithStateGroups = wh3Db.DialogueEventsWithStateGroups;
            var stateGroupsWithStates0 = wh3Db.StateGroupsWithStates0;
            var stateGroupsWithStates1 = wh3Db.StateGroupsWithStates1;
            var stateGroupsWithStates = stateGroupsWithStates0.Concat(stateGroupsWithStates1).ToList();
            ChangeNoneStatesToAny(stateGroupsWithStates);

            return (nameLookUp, dialogueEventsWithStateGroups, stateGroupsWithStates);
        }

        public Dictionary<uint, string> BuildNameHelper(SoundDatFile wh3Db)
        {
            var wh3DbNameList = wh3Db.CreateFileNameList();
            AddNames(wh3DbNameList);

            // Add all the bnk file names 
            var bnkFiles = PackFileServiceUtility.FindAllWithExtention(_pfs, ".bnk");
            var bnkNames = bnkFiles.Select(x => x.Name.Replace(".bnk", "")).ToArray();
            AddNames(bnkNames);

            var wwiseIdFiles = PackFileServiceUtility.FindAllWithExtention(_pfs, ".wwiseids");
            foreach (var item in wwiseIdFiles)
            {
                var data = Encoding.UTF8.GetString(item.DataSource.ReadData());
                data = data.Replace("\r", "");
                var splitData = data.Split("\n");
                AddNames(splitData);
            }

            return _nameLookUp;
        }

        SoundDatFile LoadDatFiles(IPackFileService pfs, out List<string> failedFiles)
        {
            var datDumpsFolderName = $"{DirectoryHelper.Temp}\\DatDumps";
            DirectoryHelper.EnsureCreated(datDumpsFolderName);

            var datFiles = PackFileServiceUtility.FindAllWithExtention(pfs, ".dat");
            datFiles = PackFileUtil.FilterUnvantedFiles(pfs, datFiles, new[] { "bank_splits.dat", "campaign_music.dat", "battle_music.dat", "icudt61l.dat" }, out var removedFiles);

            var failedDatParsing = new List<(string, string)>();
            var masterDat = new SoundDatFile();

            foreach (var datFile in datFiles)
            {
                var datDump = $"{datDumpsFolderName}\\dat_dump_{datFile}.txt";
                try
                {
                    var parsedFile = LoadDatFile(datFile);
                    masterDat.Merge(parsedFile);
                    //parsedFile.DumpToFile(datDump); // This creates dat dumps for individual dat files
                }
                catch (Exception e)
                {
                    var fullPath = pfs.GetFullPath(datFile);
                    failedDatParsing.Add((fullPath, e.Message));
                }
            }

            failedFiles = failedDatParsing.Select(x => x.Item1).ToList();

            var masterDatDump = $"{datDumpsFolderName}\\dat_dump_master.txt";
            masterDat.DumpToFile(masterDatDump);
            return masterDat;
        }

        SoundDatFile LoadDatFile(PackFile datFile)
        {
            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Attila)
                return DatFileParser.Parse(datFile, true);
            else
                return DatFileParser.Parse(datFile, false);
        }

        void AddNames(string[] names)
        {
            foreach (var name in names)
            {
                var hashVal = WwiseHash.Compute(name.Trim());
                _nameLookUp[hashVal] = name;
            }
        }

        private static void ChangeNoneStatesToAny(List<SoundDatFile.DatStateGroupsWithStates> stateGroupsWithStates)
        {
            foreach (var item in stateGroupsWithStates)
            {
                for (var i = 0; i < item.States.Count; i++)
                {
                    if (item.States[i] == "None")
                    {
                        // Replace States equal to "None" with "Any". In Wwise when no state is selected in a State Group it sets the State to "Any", for some reason CA's dat has that State called "None" which doesn't make sense.
                        // It's not as if no state is applied, what actually happens is the State Path uses 'any' State from the State Group, or in other words 'all' States not 'none'... Confusing I know.
                        item.States[i] = "Any";
                    }
                }
            }
        }
    }
}
