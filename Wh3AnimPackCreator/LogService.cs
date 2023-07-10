using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Wh3AnimPackCreator
{
    public class LogService
    {
        public enum LogType
        {
            Error,
            Warning,
            Info,
        }

        public string LogType_Error => "";
        public string LogType_Warning => "";
        public string LogType_Info => "";


        [DebuggerDisplay("LogItem {LogType} {Category} {BinName} {Description}")]
        public class LogItem
        {
            public string LogType { get; set; } = string.Empty;
            public string BinName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class StatisticsDb
        {
            public List<string> MissingAnimations { get; private set; } = new List<string>();
            public List<string> MissingSounds { get; private set; } = new List<string>();
            public List<string> MissingEffects { get; private set; } = new List<string>();
            public List<string> MissingSlots { get; private set; } = new List<string>();
            public List<string> UnsupportedMetadataTag { get; private set; } = new List<string>();

            public HashSet<string> ProcessedSlots { get; private set; } = new HashSet<string>();
            public HashSet<string> ProcessedMetadataTag { get; private set; } = new HashSet<string>();
            public HashSet<string> ProcessedEffects { get; private set; } = new HashSet<string>();
            public HashSet<string> ProcessedSounds { get; private set; } = new HashSet<string>();
        }


        List<LogItem> _log = new List<LogItem>();
        string _outputPath;
        public StatisticsDb StatsDb { get; private set; } = new StatisticsDb();

        public LogService(string outputFolder)
        {
            _outputPath = outputFolder;
            DirectoryHelper.EnsureCreated(outputFolder);
        }

        public void AddLogItem(LogType logType, string binName, string description, string category = "")
        {
            var item = new LogItem()
            {
                LogType = GetLogTypeText(logType),
                BinName = binName,
                Category = category,
                Description = description
            };
            _log.Add(item);
        }

        public void AddBinSummary(string binName, int missingSlots, int missingEffects, int missingMetaDataTags, int missingSounds, int missingFilesRemoved)
        {

        }

        public void Save()
        {
            // Save the log
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sep=|");
            sb.AppendLine("LogType|BinName|Category|Description");
            foreach (var item in _log)
                sb.AppendLine($"{item.LogType}|{item.BinName}|{item.Category}|{item.Description}");

            File.WriteAllText(_outputPath + "Log.csv", sb.ToString());

            // Create a summary and some statistics 
            sb.Clear();

            // Effects:
            var effectInfos = StatsDb.MissingEffects.GroupBy(x => x).Select(x => $"{x.Key}:{x.Count()}").ToList();
            sb.AppendLine("EFFECTS:");
            sb.AppendLine($"\tTotal missing: {StatsDb.MissingEffects.Count()}");
            sb.AppendLine($"\tTotal unique missing: {StatsDb.MissingEffects.Distinct().Count()}");
            foreach (var item in effectInfos)
                sb.AppendLine($"\t\t{item}");
            sb.AppendLine();

            // Slots
            var slotInfos = StatsDb.MissingSlots.GroupBy(x => x).Select(x => $"{x.Key}:{x.Count()}").ToList();
            sb.AppendLine("Slots:");
            sb.AppendLine($"\tTotal missing: {StatsDb.MissingSlots.Count()}");
            sb.AppendLine($"\tTotal unique missing: {StatsDb.MissingSlots.Distinct().Count()}");
            foreach (var item in slotInfos)
                sb.AppendLine($"\t\t{item}");
            sb.AppendLine();

            // Metadata
            var metaDataInfos = StatsDb.UnsupportedMetadataTag.GroupBy(x => x).Select(x => $"{x.Key}:{x.Count()}").ToList();
            sb.AppendLine("MetaData:");
            sb.AppendLine($"\tTotal missing: {StatsDb.UnsupportedMetadataTag.Count()}");
            sb.AppendLine($"\tTotal unique missing: {StatsDb.UnsupportedMetadataTag.Distinct().Count()}");
            foreach (var item in metaDataInfos)
                sb.AppendLine($"\t\t{item}");
            sb.AppendLine();


            // Sounds
            // Animations

            File.WriteAllText(_outputPath + "AnimPackBatchConverterSummary.txt", sb.ToString());
        }

        string GetLogTypeText(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return "Error";
                case LogType.Info: return "Info";
                case LogType.Warning: return "Warning";
                default: throw new Exception("Type not implemented");
            }
        }
    }
}
