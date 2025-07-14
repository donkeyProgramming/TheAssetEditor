using System.Text;
using CommonControls.BaseDialogs;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Reports.DeepSearch
{

    public class DeepSearchCommand(DeepSearchReport deepSearchReport) : IUiCommand
    {
        public void Execute()
        {
            var window = new TextInputWindow("Deep search - Output in console", "");
            if (window.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(window.TextValue))
                {
                    System.Windows.MessageBox.Show("Invalid input");
                    return;
                }
                deepSearchReport.DeepSearch(window.TextValue, false);
            }
        }
    }

    public class DeepSearchReport
    {
        private readonly ILogger _logger = Logging.Create<DeepSearchReport>();
        private readonly IPackFileService _packFileService;

        public DeepSearchReport(IPackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public List<string> DeepSearch(string searchStr, bool caseSensetive)
        {
            _logger.Here().Information($"Searching for : '{searchStr}'");
            var packFiles = _packFileService.GetAllPackfileContainers();

            var filesWithResult = new List<KeyValuePair<string, string>>();
            var files = packFiles.SelectMany(x => x.FileList.Select(x => (x.Value.DataSource as PackedFileSource).Parent.FilePath)).Distinct().ToList();

            var indexLock = new object();
            var currentPackFileIndex = 0;

            Parallel.For(0, files.Count,
              index =>
              {
                  var currentIndex = 0;

                  lock (indexLock)
                  {
                      currentIndex = currentPackFileIndex;
                      currentPackFileIndex++;
                  }

                  var packFilePath = files[currentIndex];
                  if (packFilePath.Contains("audio", StringComparison.InvariantCultureIgnoreCase))
                  {
                      _logger.Here().Information($"Skipping audio file {currentIndex}/{files.Count}");
                  }
                  else
                  {
                      using (var fileStram = File.OpenRead(packFilePath))
                      {
                          using (var reader = new BinaryReader(fileStram, Encoding.ASCII))
                          {
                              var pfc = PackFileSerializer.Load(packFilePath, reader, new CaPackDuplicatePackFileResolver());

                              _logger.Here().Information($"Searching through packfile {currentIndex}/{files.Count} -  {packFilePath} {pfc.FileList.Count} files");

                              foreach (var packFile in pfc.FileList.Values)
                              {
                                  var pf = packFile;
                                  var ds = pf.DataSource as PackedFileSource;
                                  var bytes = ds.ReadData(fileStram);
                                  var str = Encoding.ASCII.GetString(bytes);

                                  if (str.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
                                  {
                                      var fillPathFile = pfc.FileList.FirstOrDefault(x => x.Value == packFile).Key;
                                      _logger.Here().Information($"Found result in '{fillPathFile}' in '{packFilePath}'");

                                      lock (filesWithResult)
                                      {
                                          filesWithResult.Add(new KeyValuePair<string, string>(fillPathFile, packFilePath));
                                      }
                                  }
                              }
                          }
                      }
                  }
              });

            _logger.Here().Information($"[{filesWithResult.Count}] Result for '{searchStr}'_________________:");
            foreach (var item in filesWithResult)
                _logger.Here().Information($"\t\t'{item.Key}' in '{item.Value}'");

            return filesWithResult.Select(x => x.Value).ToList();
        }
    }
}
