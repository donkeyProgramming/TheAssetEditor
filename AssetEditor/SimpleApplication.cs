using AssetEditor;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AudioResearch
{
    public class SimpleApplication : IDisposable
    {
        public bool SkipLoadingWemFiles { get; set; } = true;
        public bool LoadAllCaFiles { get; set; } = true;
        IServiceScope _serviceScope;

        public SimpleApplication()
        {
            var applicationBuilder = new DependencyInjectionConfig();
            _serviceScope = applicationBuilder.ServiceProvider.CreateScope();

            // Configure based on settings
            var settingsService = _serviceScope.ServiceProvider.GetService<ApplicationSettingsService>();
            settingsService.CurrentSettings.SkipLoadingWemFiles = SkipLoadingWemFiles;

            if (LoadAllCaFiles)
            {
                var pfs = GetService<PackFileService>();
                pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            }
        }

        public T GetService<T>() => _serviceScope.ServiceProvider.GetService<T>();

        public void Dispose() => _serviceScope.Dispose();
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
