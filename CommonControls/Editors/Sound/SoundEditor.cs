using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Diagnostics;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;
using Serilog;
using System.Timers;
using System.IO;
using System.Text;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.Common;

namespace CommonControls.Editors.Sound
{
    public partial class SoundEditor
    {
        ILogger _logger = Logging.Create<SoundEditor>();

        private readonly PackFileService _pfs;
        List<string> _filesToSkip = new List<string>();

        public SoundEditor(PackFileService pfs)
        {
            _pfs = pfs;

            _filesToSkip.Add("media");
            _filesToSkip.Add("init.bnk");
            _filesToSkip.Add("animation_blood_data.bnk");
        }

        public void CreateSoundMap()
        {
            bool hardCodedAttilaMode = true;
            List<PackFile> files;
            if(hardCodedAttilaMode)
                files = GetAttilaFiles();
            else
                files = GetPackFileFiles();

            var nameHelper = GetNameHelper(files, @"C:\temp\SoundTesting\AttilaEvents.txt", hardCodedAttilaMode);

            var timer = new Stopwatch();
            timer.Start();

            VisualEventOutputNode rootOutput = new VisualEventOutputNode($"Root :");
            var statsNode = rootOutput.AddChild("Stats");
            //files = OnlyParseOneFile_debug(files, "battle_vo_orders__core.bnk");
            //files = OnlyParseOneFile_debug(files, "battle_advice.bnk"); // Attila
            //files = OnlyParseOneFile_debug(files, "battle_animation.bnk"); // Attila


            files = RemoveUnwantedFiles(files, rootOutput, timer);
            var masterDb = BuildMasterDb(files, rootOutput, timer);
            ParsBnkFiles(masterDb, nameHelper, files, rootOutput, timer);
            AddStats(statsNode, masterDb, files.Count);

            VisualEventSerializer serializer = new VisualEventSerializer();
            var output = serializer.Start(rootOutput);

            File.WriteAllText(@"C:\temp\SoundTesting\AttilaSoundTree.txt", output);
            CreateHircList(masterDb, @"C:\temp\SoundTesting\AttilaHircList.txt", nameHelper);
        }

        List<PackFile> GetAttilaFiles()
        {
            var fileInFolder = Directory.GetFiles(@"C:\temp\SoundTesting\Attila");
            fileInFolder = fileInFolder.Where(x => x.Contains(".bnk", StringComparison.OrdinalIgnoreCase)).ToArray();

            var packfilList = new List<PackFile>();
            foreach (var file in fileInFolder)
            {
                var tempFile = new PackFile(Path.GetFileName(file), new FileSystemSource(file));
                packfilList.Add(tempFile);
            }
            return packfilList;
        }

        List<PackFile> GetPackFileFiles()
        {
            var files = _pfs.FindAllWithExtention(".bnk");
            return files;
        }

        NameLookupHelper GetNameHelper(List<PackFile> files, string savePath, bool onlyAttilaHardCoded = false)
        {
            var masterDat = new SoundDatFile();
            
            var datPackFiles = _pfs.FindAllWithExtention(".dat");

            if (onlyAttilaHardCoded)
                datPackFiles.Clear();

            foreach (var datPackFile in datPackFiles)
            {
                var datFile = DatParser.Parse(datPackFile, false);
                masterDat.Merge(datFile);
            }
            
            var attillaFile = new PackFile(Path.GetFileName(@"Event_data.dat"), new FileSystemSource(@"C:\temp\SoundTesting\Attila\event_data.dat"));
            var attilaDatFile = DatParser.Parse(attillaFile, true);
            masterDat.Merge(attilaDatFile);

          

            var fileNameDump0 = files.Select(x => x.Name);
            var fileNameDump1 = files.Select(x => Path.GetFileNameWithoutExtension(x.Name));
            var fileNameDump3 = fileNameDump0.Union(fileNameDump1);
            var fileNameDump4 = string.Join(", \n", fileNameDump3);

            foreach (var item in fileNameDump3)
                masterDat.Event3.Add(new SoundDatFile.EventType3() { EventName = item });

            var nameHelper = new NameLookupHelper(masterDat.CreateFileNameList());

            masterDat.SaveTextVersion(savePath, nameHelper);
            return nameHelper;
        }

        List<PackFile> RemoveUnwantedFiles(List<PackFile> files, VisualEventOutputNode parent, Stopwatch timer)
        {
            _logger.Here().Information($"Removing unwanted files [{timer.Elapsed.TotalSeconds}s]");

            var itemsToRemove = new List<PackFile>();
            foreach (var file in files)
            {
                foreach (var removeName in _filesToSkip)
                {
                    if (file.Name.Contains(removeName))
                    {
                        itemsToRemove.Add(file);
                        break;
                    }
                }
            }

            if (itemsToRemove.Count != 0)
            {
                var root = parent.AddChild($"RemovedFiles: {itemsToRemove.Count}");
                foreach (var item in itemsToRemove)
                {
                    root.AddChild(item.Name);
                    files.Remove(item);
                }
            }

            _logger.Here().Information($"{itemsToRemove.Count} files removed [{timer.Elapsed.TotalSeconds}s]");

            return files;
        }

        List<PackFile> OnlyParseOneFile_debug(List<PackFile> files, string filter)
        {
            var itemsToRemove = new List<PackFile>();
            foreach (var file in files)
            {
                if (file.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) == false)
                    itemsToRemove.Add(file);
            }

            if (itemsToRemove.Count != 0)
            {
                foreach (var item in itemsToRemove)
                    files.Remove(item);
            }
            return files;
        }

       ExtenededSoundDataBase BuildMasterDb(List<PackFile> files, VisualEventOutputNode parent, Stopwatch timer)
        {
            _logger.Here().Information($"Generating Master DB [{timer.Elapsed.TotalSeconds}s]");

            var fileRoot = parent.AddChild("File information:");
            var masterDb = new ExtenededSoundDataBase();
            
            var currentFile = 0;
            foreach (var file in files)
            {
                try
                {
                    var localDb = Bnkparser.Parse(file);
                    var eventCount = localDb.Hircs.Count(x => x.Type == HircType.Event);
                    var dialogEventCount = localDb.Hircs.Count(x => x.Type == HircType.Dialogue_Event);

                    var fileOutputStr = $"{file.Name} Events:{eventCount} DialogEvents: {dialogEventCount}";  // Some kind of failed items/unsupporeted item log as well
                    fileRoot.AddChild(fileOutputStr);

                    masterDb.AddHircItems(localDb.Hircs);

                    _logger.Here().Information($"{currentFile}/{files.Count} {fileOutputStr}");
                }
                catch (Exception e)
                {
                    _logger.Here().Information($"{currentFile}/{files.Count} {file.Name} Error:{e.Message}");
                    fileRoot.AddChild($"{file.Name} Error:{e.Message}");
                }

                currentFile++;
            }

            _logger.Here().Information($"Generating Master DB Done [{timer.Elapsed.TotalSeconds}s]");

            return masterDb;
        }

        void ParsBnkFiles(ExtenededSoundDataBase masterDb, NameLookupHelper nameHelper, List<PackFile> files, VisualEventOutputNode parent, Stopwatch timer)
        {
            /*for(int fileIndex = 0; fileIndex < files.Count; fileIndex++)
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
            }*/
        }

        public void CreateHircList(ExtenededSoundDataBase db, string outputName, NameLookupHelper nameLookupHelper)
        {
            StringBuilder output = new StringBuilder();

            var totalItems = db.HircList.SelectMany(x => x.Value.Select(y => y.Id)).ToList();
            var totalUniqueItems = totalItems.Distinct();

            var flatList = db.HircList.SelectMany(x => x.Value).ToList();
            var grouped = flatList.GroupBy(x => x.Type).ToList();
            var overviewData = grouped.Select(x => new
            {
                Name = x.Key,
                TotalCount = x.Count(),
                UnknownCount = x.Count(y => y is FileTypes.Sound.WWise.Hirc.CAkUnknown),
                ErrorCount = x.Count(y => y.HasError)
            }).ToList();

            var failCount = flatList.Count(x => x.HasError);
            var unknownCount = flatList.Count(x => x is FileTypes.Sound.WWise.Hirc.CAkUnknown);

            output.AppendLine($"Hitc Item count: {totalItems.Count}");
            output.AppendLine($"Distinct Hitc count: {totalUniqueItems.Count()}");
            output.AppendLine($"Failed count: {failCount}");
            output.AppendLine($"Unknown count: {unknownCount}");

            output.AppendLine("\n-----------------------------\n");
            foreach (var item in overviewData)
                output.AppendLine($"{item.Name} - Errors: {item.ErrorCount} Unkowns: {item.UnknownCount}");
            output.AppendLine("\n-----------------------------\n");

           foreach(var itemCollection in db.HircList)
            {
                var displayName = nameLookupHelper.GetName(itemCollection.Key);
                var numChildren = itemCollection.Value.Count();
                if (numChildren == 1)
                {
                    var instance = itemCollection.Value.First();
                    output.AppendLine($"Id:{itemCollection.Key} [{displayName}] - {instance.Type} {instance.OwnerFile} {instance.IndexInFile} ");
                }
                else 
                {
                    output.AppendLine($"Id:{itemCollection.Key} [{displayName}] Multiple instances {itemCollection.Value.Count}");
                    foreach(var instance in itemCollection.Value)
                        output.AppendLine($"\t {instance.Type} {instance.OwnerFile} {instance.IndexInFile} ");
                }
            }

            File.WriteAllText(outputName, output.ToString());
        }

        public void AddStats(VisualEventOutputNode statsNode, ExtenededSoundDataBase masterDb, int numFiles)
        {
            var unknowTypeInfo = masterDb.UnknownObjectTypes.Distinct().Select(x => x + $"[{masterDb.UnknownObjectTypes.Count(unkObj => unkObj == x)}]");
            var numWemFiles = _pfs.FindAllWithExtention(".wem");

            statsNode.AddChild($"Num bnk Files = {numFiles}");
            statsNode.AddChild($"Num wem Files = {numWemFiles.Count}");
            statsNode.AddChild($"References wem Files = {masterDb.ReferensedSounds.Distinct().Count()}");
            statsNode.AddChild($"Unknown hirc types = {string.Join(",", unknowTypeInfo)}");
        }
    }
}

/*
 * 
 * Final test, add a new sound in meta tabel Karl franze running : "Look at me....Wiiiii" 
 * Vocalisation_dlc14_medusa_idle_hiss
 * 
    event > action > sound > .wem
    event > action > random-sequence > sound(s) > .wem
    event > action > switch > switch/segment/sound > ...
    event > action > music segment > music track(s) > .wem(s).
    event > action > music random-sequence > music segment(s) > ...
    event > action > music switch > switch(es)/segment(s)/random-sequence(s) > ...


    Event => action     =>  sound
                        =>  CAkActionSetAkProp
                        =>  Switch  => sound
                                    => Rand

                        =>  Rand    => Sound
 */


