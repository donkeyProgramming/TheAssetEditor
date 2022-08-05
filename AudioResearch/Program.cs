using CommonControls.Editors.AudioEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioResearch
{

    // RPFC - Real time parameter control 
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            string[] allBnkFiles = GetAllBnkFiles();
            var datDb = CreateDatDb();

            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(new BnkFileProvider(GetAllBnkFiles()));
            Console.ReadLine();
        }

        static string[] FilterUnvantedFiles(string[] files, string[] removeFilters, out string[] removedFiles)
        {
            var tempRemoveFiles = new List<string>();
            var fileList = files.ToList();

            // Files that contains multiple items not decoded.

            foreach (var file in fileList)
            {
                foreach (var removeName in removeFilters)
                {
                    if (file.Contains(removeName))
                    {
                        tempRemoveFiles.Add(file);
                        break;
                    }
                }
            }

            foreach (var item in tempRemoveFiles)
                fileList.Remove(item);

            removedFiles = tempRemoveFiles.ToArray();
            return fileList.ToArray();
        }

        static string GetExportedWWiseFolder()
        {
            var possibleFolders = new[] { @"C:\Users\ole_k\Desktop\Wh3 sounds\audio\wwise", @"c:\KlissiansFolders.." };
            foreach (var folder in possibleFolders)
            {
                if (Directory.Exists(folder))
                    return folder;
            }

            throw new Exception("Exported folder not found. Please add to the array above...");
        }

        private static string[] GetAllBnkFiles()
        {
            var allBnkFiles = Directory.GetFiles(GetExportedWWiseFolder(), "*.bnk", SearchOption.AllDirectories);
            if (allBnkFiles.Length != 611)
                throw new Exception("The export folder should contain 611 bnk files. (Core files + English Folder)");
            return allBnkFiles;
        }

        private static string[] GetAllDatFiles()
        {
            var allDatFiles = Directory.GetFiles(GetExportedWWiseFolder(), "*.dat", SearchOption.AllDirectories);
            if (allDatFiles.Length != 15)
                throw new Exception("The export folder should contain 15 dat files.");
            return allDatFiles;
        }

        private static SoundDatFile CreateDatDb()
        {
            var datFiles = GetAllDatFiles();
            datFiles = FilterUnvantedFiles(datFiles, new[] { "bank_splits.dat", "campaign_music.dat", "battle_music.dat" }, out var removedFiles);     

            var failedDatParsing = new List<(string, string)>();        
            var masterDat = new SoundDatFile();
            foreach (var datFile in datFiles)
            {
                try
                {
                    var pf = new PackFile(datFile, new FileSystemSource(datFile));
                    var parsedFile = DatParser.Parse(pf, false);
                    masterDat.Merge(parsedFile);
                }
                catch (Exception e)
                {
                    failedDatParsing.Add((datFile, e.Message));
                }
            }
            return masterDat;
        }

        class BnkFileProvider : IBnkProvider
        {
            private readonly string[] _diskPaths;

            public BnkFileProvider(string[] diskPaths)
            {
                _diskPaths = diskPaths;
            }

            public List<PackFile> GetBnkFiles() => _diskPaths.Select(x => new PackFile(x, new FileSystemSource(x))).ToList();
            public string GetFullName(PackFile pf) => pf.Name;
        }

    }


    /*
      void ParsBnkFiles(ExtenededSoundDataBase masterDb, NameLookupHelper nameHelper, List<PackFile> files, VisualEventOutputNode parent, Stopwatch timer)
        {
            for(int fileIndex = 0; fileIndex < files.Count; fileIndex++)
            {
                try
                {
                    var soundDb = Bnkparser.Parse(files[fileIndex]);

                    var events = soundDb.Hircs
                        .Where(x => x.Type == HircType.Event || x.Type == HircType.Dialogue_Event)
                        .Where(x => x.HasError == false);

                    var eventsCount = events.Count();
                    var fileNodeOutputStr = $"{files[fileIndex].Name} Total EventCount:{eventsCount}";
                    _logger.Here().Information($"{fileIndex}/{files.Count} {fileNodeOutputStr}");

                    var fileOutput = parent.AddChild(fileNodeOutputStr);
                    var fileOutputError = fileOutput.AddChild("Errors while parsing :");
                    bool procesedCorrectly = true;

                    var itemsProcessed = 0;
                    foreach (var currentEvent in events)
                    {
                        var visualEvent = new EventHierarchy(currentEvent, masterDb, nameHelper, fileOutput, fileOutputError, files[fileIndex].Name);

                        if (itemsProcessed % 100 == 0 && itemsProcessed != 0)
                            _logger.Here().Information($"\t{itemsProcessed}/{eventsCount} events processsed [{timer.Elapsed.TotalSeconds}s]");

                        itemsProcessed++;
                        procesedCorrectly = visualEvent.ProcesedCorrectly && procesedCorrectly;
                    }

                    if (procesedCorrectly == true)
                        fileOutput.Children.Remove(fileOutputError);

                    if (events.Any())
                        _logger.Here().Information($"\t{itemsProcessed}/{eventsCount} events processsed [{timer.Elapsed.TotalSeconds}s]");
                }
                catch
                { }
            }
        }
     */
}
