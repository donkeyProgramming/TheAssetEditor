using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Editors.Audio.Shared.Wwise;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Dat;
using static Shared.GameFormats.Dat.SoundDatFile;

namespace Editors.Audio.Shared.Storage
{
    public class DatLoader(IPackFileService pfs, ApplicationSettingsService applicationSettingsService)
    {
        public class Result
        {
            public Dictionary<uint, string> NameById { get; set; } = [];
            public Dictionary<string, List<string>> StateGroupsByDialogueEvent { get; set; } = [];
            public Dictionary<string, Dictionary<string, string>> QualifiedStateGroupByStateGroupByDialogueEvent { get; set; } = [];
            public Dictionary<string, List<string>> StatesByStateGroup { get; set; } = [];
        }

        private readonly IPackFileService _pfs = pfs;
        private readonly ApplicationSettingsService _applicationSettingsService = applicationSettingsService;

        public Result LoadDatData()
        {
            var datDb = LoadDatFiles(_pfs, out var _);
            var nameLookUp = BuildNameHelper(datDb);

            var unprocessedDialogueEventsWithStateGroups = datDb.DialogueEventsWithStateGroups;
            var processedDialogueEventsWithStateGroups = ProcessDialogueEvents(unprocessedDialogueEventsWithStateGroups, nameLookUp);

            // Add qualifiers to State Groups as some events have the same State Group twice e.g. VO_Actor.
            var dialogueEventsWithStateGroupsWithQualifiersAndStateGroups = BuildDialogueEventsWithStateGroupsWithQualifiersAndStateGroups(processedDialogueEventsWithStateGroups);

            var stateGroupsWithStates0 = datDb.StateGroupsWithStates0;
            var stateGroupsWithStates1 = datDb.StateGroupsWithStates1;
            var unprocessedStateGroupsWithStates = stateGroupsWithStates0.Concat(stateGroupsWithStates1).ToList();
            var processedStateGroupsWithStates = ProcessStateGroups(unprocessedStateGroupsWithStates);

            return new Result
            {
                NameById = nameLookUp,
                StateGroupsByDialogueEvent = processedDialogueEventsWithStateGroups,
                QualifiedStateGroupByStateGroupByDialogueEvent = dialogueEventsWithStateGroupsWithQualifiersAndStateGroups,
                StatesByStateGroup = processedStateGroupsWithStates,
            };
        }

        private static Dictionary<string, List<string>> ProcessDialogueEvents(List<DatDialogueEventsWithStateGroups> dialogueEvents, Dictionary<uint, string> nameLookup)
        {
            var processedDialogueEventsWithStateGroups = new Dictionary<string, List<string>>();

            foreach (var dialogueEvent in dialogueEvents)
            {
                if (!processedDialogueEventsWithStateGroups.ContainsKey(dialogueEvent.Event))
                    processedDialogueEventsWithStateGroups[dialogueEvent.Event] = new List<string>();

                foreach (var stateGroupId in dialogueEvent.StateGroups)
                {
                    if (nameLookup.TryGetValue(stateGroupId, out var stateGroup))
                        processedDialogueEventsWithStateGroups[dialogueEvent.Event].Add(stateGroup);
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
                if (!processedStateGroupsWithStates.ContainsKey(stateGroup.StateGroup))
                    processedStateGroupsWithStates[stateGroup.StateGroup] = new List<string>();

                processedStateGroupsWithStates[stateGroup.StateGroup].AddRange(stateGroup.States);
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

            var languages = new List<string> { "sfx", "chinese", "english(uk)", "french(france)", "german", "italian", "polish", "russian", "spanish(spain)" }.ToArray();
            AddNames(languages, nameLookUp);

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
            var datFiles = PackFileServiceUtility.FindAllWithExtention(pfs, ".dat");
            datFiles = PackFileUtil.FilterUnvantedFiles(pfs, datFiles, ["bank_splits.dat", "campaign_music.dat", "battle_music.dat", "icudt61l.dat"], out var removedFiles);

            var failedDatParsing = new List<(string, string)>();
            var masterDat = new SoundDatFile();

            foreach (var datFile in datFiles)
            {
                try
                {
                    var parsedFile = LoadDatFile(datFile);
                    masterDat.Merge(parsedFile);
                }
                catch (Exception e)
                {
                    var fullPath = pfs.GetFullPath(datFile);
                    failedDatParsing.Add((fullPath, e.Message));
                }
            }

            failedFiles = failedDatParsing.Select(x => x.Item1).ToList();
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
                        // In Wwise when no State is selected in a state group it displays this as "Any" state, i.e. the path continues for any State.
                        // CA call that "Any" State "None" according to the .dat file.
                        // "Any" makes more sense than "None" so we replace states equal to "None" with "Any".
                        item.States[i] = "Any";
                    }
                }
            }
            return stateGroupsWithStates;
        }
    }
}
