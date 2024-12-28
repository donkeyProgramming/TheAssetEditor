using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Dat;
using static Shared.GameFormats.Dat.SoundDatFile;

namespace Editors.Audio.Storage
{
    public class DatLoader
    {
        public class LoadResult
        {
            public Dictionary<uint, string> NameLookUpTable { get; set; } = [];
            public Dictionary<string, List<string>> DialogueEventsWithStateGroups { get; set; } = [];
            public Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiersAndStateGroups { get; set; } = [];
            public Dictionary<string, List<string>> StateGroupsWithStates { get; set; } = [];
        }

        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public DatLoader(IPackFileService pfs, ApplicationSettingsService applicationSettingsService)
        {
            _pfs = pfs;
            _applicationSettingsService = applicationSettingsService;
        }

        public LoadResult LoadDatData()
        {
            var wh3Db = LoadDatFiles(_pfs, out var _);
            var nameLookUp = BuildNameHelper(wh3Db);

            var unprocessedDialogueEventsWithStateGroups = wh3Db.DialogueEventsWithStateGroups;
            var processedDialogueEventsWithStateGroups = ProcessDialogueEvents(unprocessedDialogueEventsWithStateGroups, nameLookUp);

            // Add qualifiers to State Groups as some events have the same State Group twice e.g. VO_Actor.
            var dialogueEventsWithStateGroupsWithQualifiersAndStateGroups = BuildDialogueEventsWithStateGroupsWithQualifiersAndStateGroups(processedDialogueEventsWithStateGroups);

            var stateGroupsWithStates0 = wh3Db.StateGroupsWithStates0;
            var stateGroupsWithStates1 = wh3Db.StateGroupsWithStates1;
            var unprocessedStateGroupsWithStates = stateGroupsWithStates0.Concat(stateGroupsWithStates1).ToList();
            var processedStateGroupsWithStates = ProcessStateGroups(unprocessedStateGroupsWithStates);

            return new LoadResult
            {
                NameLookUpTable = nameLookUp,
                DialogueEventsWithStateGroups = processedDialogueEventsWithStateGroups,
                DialogueEventsWithStateGroupsWithQualifiersAndStateGroups = dialogueEventsWithStateGroupsWithQualifiersAndStateGroups,
                StateGroupsWithStates = processedStateGroupsWithStates
            };
        }

        private static Dictionary<string, List<string>> ProcessDialogueEvents(List<DatDialogueEventsWithStateGroups> dialogueEvents, Dictionary<uint, string> nameLookup)
        {
            var processedDialogueEventsWithStateGroups = new Dictionary<string, List<string>>();

            foreach (var dialogueEvent in dialogueEvents)
            {
                if (!processedDialogueEventsWithStateGroups.ContainsKey(dialogueEvent.EventName))
                    processedDialogueEventsWithStateGroups[dialogueEvent.EventName] = new List<string>();

                foreach (var stateGroupId in dialogueEvent.StateGroups)
                {
                    if (nameLookup.TryGetValue(stateGroupId, out var stateGroup))
                        processedDialogueEventsWithStateGroups[dialogueEvent.EventName].Add(stateGroup);
                }
            }
            return processedDialogueEventsWithStateGroups;
        }

        private static Dictionary<string, List<string>> ProcessStateGroups(List<DatStateGroupsWithStates> unprocessedStateGroupsWithStates)
        {
            var processedStateGroupsWithStates = new Dictionary<string, List<string>>();

            var stateGroups = ChangeNoneStatesToAny(unprocessedStateGroupsWithStates);
            foreach (var stateGroup in stateGroups)
            {
                if (!processedStateGroupsWithStates.ContainsKey(stateGroup.StateGroupName))
                    processedStateGroupsWithStates[stateGroup.StateGroupName] = new List<string>();

                processedStateGroupsWithStates[stateGroup.StateGroupName].AddRange(stateGroup.States);
            }

            return processedStateGroupsWithStates;
        }

        public static Dictionary<string, Dictionary<string, string>> BuildDialogueEventsWithStateGroupsWithQualifiersAndStateGroups(Dictionary<string, List<string>> dialogueEventsWithStateGroups)
        {
            var dialogueEventsWithStateGroupsWithQualifiersAndStateGroups = new Dictionary<string, Dictionary<string, string>>();

            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                var stateGroupsWithQualifiers = new Dictionary<string, string>();
                var stateGroups = dialogueEvent.Value;

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in stateGroups)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        var qualifier = voActorCount > 1 ? "VO_Actor (Target)" : "VO_Actor (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Actor";
                    }
                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        var qualifier = voCultureCount > 1 ? "VO_Culture (Target)" : "VO_Culture (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Culture";
                    }
                    else
                        stateGroupsWithQualifiers[stateGroup] = stateGroup; // No qualifier needed as State Group doesn't reoccur.
                }

                dialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Key] = stateGroupsWithQualifiers;
            }

            return dialogueEventsWithStateGroupsWithQualifiersAndStateGroups;
        }

        public Dictionary<uint, string> BuildNameHelper(SoundDatFile wh3Db)
        {
            var nameLookUp = new Dictionary<uint, string>();
            var wh3DbNameList = wh3Db.CreateFileNameList();
            AddNames(wh3DbNameList, nameLookUp);

            var bnkFiles = PackFileServiceUtility.FindAllWithExtention(_pfs, ".bnk");
            var bnkNames = bnkFiles.Select(x => x.Name.Replace(".bnk", "")).ToArray();
            AddNames(bnkNames, nameLookUp);

            var wwiseIdFiles = PackFileServiceUtility.FindAllWithExtention(_pfs, ".wwiseids");
            foreach (var item in wwiseIdFiles)
            {
                var data = Encoding.UTF8.GetString(item.DataSource.ReadData());
                data = data.Replace("\r", "");
                var splitData = data.Split("\n");
                AddNames(splitData, nameLookUp);
            }

            return nameLookUp;
        }

        private SoundDatFile LoadDatFiles(IPackFileService pfs, out List<string> failedFiles)
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

        private SoundDatFile LoadDatFile(PackFile datFile)
        {
            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Attila)
                return DatFileParser.Parse(datFile, true);
            else
                return DatFileParser.Parse(datFile, false);
        }

        private static void AddNames(string[] names, Dictionary<uint, string> nameLookUp)
        {
            foreach (var name in names)
            {
                var hashVal = WwiseHash.Compute(name.Trim());
                nameLookUp[hashVal] = name;
            }
        }

        private static List<DatStateGroupsWithStates> ChangeNoneStatesToAny(List<DatStateGroupsWithStates> stateGroupsWithStates)
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
            return stateGroupsWithStates;
        }
    }
}
