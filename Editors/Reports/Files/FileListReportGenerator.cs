using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.Reports.Files
{
    public class FileListReportCommand(FileListReportGenerator generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class FileListReportGenerator
    {
        class FileItem
        {
            public string FileName { get; set; }
            public string Extention { get; set; } = "";
            public string ExtentionLast { get; set; } = "";
            public long Size { get; set; }
            public string PackFileName { get; set; } = "";
            public string CheckSum { get; set; }
            public bool IsDb { get; set; } = false;
        }

        private readonly ILogger _logger = Logging.Create<FileListReportGenerator>();
        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _settingsService;
        private readonly HashAlgorithm _md5Instance;

        public FileListReportGenerator(IPackFileService pfs, ApplicationSettingsService settingsService )
        {
            _pfs = pfs;
            _settingsService = settingsService;
            _md5Instance = MD5.Create();
        }

        public static void Generate(IPackFileService pfs, ApplicationSettingsService settingsService)
        {
            var instance = new FileListReportGenerator(pfs, settingsService);
            instance.Create();
        }

        public string Create()
        {
            var outputFolder = DirectoryHelper.ReportsDirectory + "\\FileList";
            DirectoryHelper.EnsureCreated(outputFolder);
            var gameName = GameInformationDatabase.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var outputFileName = $"{gameName} {DateTime.Now.ToString("yyyyMMddHHmmssfff")}.csv";
            var outputFilePath = $"{outputFolder}\\{outputFileName}";

            var containers = _pfs.GetAllPackfileContainers();
            var fileCount = containers.Sum(x => x.FileList.Count());
            _logger.Here().Information($"Creating file list report for {fileCount} files. Result will be saved at {outputFilePath}.)");

            var counter = 0;
            using var writer = new StreamWriter(outputFilePath, false);

            WriteHeader(writer);

            foreach (var container in containers)
            {
                foreach (var filePair in container.FileList)
                {
                    var fileItem = CreateFileItemFromFile(filePair.Key, filePair.Value);
                    WriteItem(writer, fileItem);

                    if (counter % 1000 == 0)
                        _logger.Here().Information($"Files processed {counter}/{fileCount} = {counter / (float)fileCount * 100}%");

                    counter++;
                }
            }

            MessageBox.Show($"Done - Created at {outputFilePath}");
            Process.Start("explorer.exe", outputFilePath);
            return outputFilePath;
        }

        bool IsDb(string fileName) => fileName.StartsWith(@"db\", StringComparison.InvariantCultureIgnoreCase);
        string LastExtention(string fileName) => Path.GetExtension(fileName);

        FileItem CreateFileItemFromFile(string fileName, PackFile file)
        {
            var extention = Regex.Match(fileName, @"\..*").Value;
            var extentionLast = LastExtention(fileName);
            var size = file.DataSource.Size;
            var checkSum = _md5Instance.ComputeHash(file.DataSource.ReadData());
            var isDb = IsDb(fileName);
            var checkSumStr = BitConverter.ToString(checkSum).Replace("-", "").ToLowerInvariant();
            var packFileName = "";
            if (file.DataSource is PackedFileSource fileSource)
                packFileName = fileSource.Parent.FilePath;

            return new FileItem()
            {
                FileName = fileName,
                Extention = extention,
                ExtentionLast = extentionLast,
                Size = size,
                CheckSum = checkSumStr,
                IsDb = isDb,
                PackFileName = packFileName
            };
        }

        public void CompareFiles(string oldFilePath, string newFilePath)
        {
            var oldData = LoadFile(oldFilePath);
            var newData = LoadFile(newFilePath);

            var gameName = GameInformationDatabase.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var gameFolder = $"{gameName}_fileCompare_{DateTime.Now.ToString("yyyyMMddHHmmss")}";

            var outputFolder = DirectoryHelper.ReportsDirectory + "\\FileList\\" + gameFolder;
            DirectoryHelper.EnsureCreated(outputFolder);
            _logger.Here().Information($"Creating file list report. Result will be saved at {outputFolder}.)");

            // Changed and added
            // ----------------------------
            var changedFiles = new List<FileItem>();
            var addedFiles = new List<FileItem>();
            var removedFiles = new List<FileItem>();
            foreach (var newItem in newData)
            {
                if (oldData.ContainsKey(newItem.Key))
                {
                    var oldItem = oldData[newItem.Key];
                    if (oldItem.CheckSum != newItem.Value.CheckSum)
                        changedFiles.Add(newItem.Value);
                }

                if (oldData.ContainsKey(newItem.Key) == false)
                    addedFiles.Add(newItem.Value);
            }

            foreach (var oldItem in oldData)
            {
                if (newData.ContainsKey(oldItem.Key) == false)
                    removedFiles.Add(oldItem.Value);
            }

            if (changedFiles.Count != 0)
            {
                using var fileWriter = new StreamWriter($"{outputFolder}\\ChangedFiles.csv", false);
                WriteHeader(fileWriter);
                foreach (var item in changedFiles)
                    WriteItem(fileWriter, item);
            }

            if (addedFiles.Count != 0)
            {
                using var fileWriter = new StreamWriter($"{outputFolder}\\AddedFiles.csv", false);
                WriteHeader(fileWriter);
                foreach (var item in addedFiles)
                    WriteItem(fileWriter, item);
            }

            if (removedFiles.Count != 0)
            {
                using var fileWriter = new StreamWriter($"{outputFolder}\\RemovedFiles.csv", false);
                WriteHeader(fileWriter);
                foreach (var item in removedFiles)
                    WriteItem(fileWriter, item);
            }

            Process.Start("explorer.exe", outputFolder);
        }

        Dictionary<string, FileItem> LoadFile(string path)
        {
            using var filestream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var fileReader = new StreamReader(filestream, Encoding.UTF8, true, 1024);

            var version = DetermineVersion(fileReader);
            var output = new Dictionary<string, FileItem>();
            var currentLine = "";

            while ((currentLine = fileReader.ReadLine()) != null)
            {
                var currentItem = LoadFromCsv(currentLine, version);
                if (output.ContainsKey(currentItem.FileName))
                {
                    _logger.Here().Error($"Multiple items with same name {currentItem.FileName}");
                    continue;
                }

                output[currentItem.FileName] = currentItem;
            }

            return output;
        }


        int DetermineVersion(StreamReader fileReader)
        {
            var line = fileReader.ReadLine();
            if (line.Contains("sep", StringComparison.InvariantCultureIgnoreCase))
            {
                var header = fileReader.ReadLine();
                if (header.Contains("ExtentionLast", StringComparison.InvariantCultureIgnoreCase) && !header.Contains("IsDb", StringComparison.InvariantCultureIgnoreCase))
                    return 1;

                if (header.Contains("ExtentionLast", StringComparison.InvariantCultureIgnoreCase) && header.Contains("IsDb", StringComparison.InvariantCultureIgnoreCase))
                    return 2;

                return 0;
            }

            throw new Exception("Unable to determine version");
        }

        FileItem LoadFromCsv(string line, int version)
        {
            var parts = line.Split('|');
            if (version == 0)
            {
                return new FileItem()
                {
                    FileName = parts[0],
                    Extention = parts[1],
                    ExtentionLast = LastExtention(parts[0]),
                    Size = long.Parse(parts[2]),
                    PackFileName = parts[3],
                    CheckSum = parts[4],
                    IsDb = IsDb(parts[0]),
                };
            }
            else if (version == 1)
            {
                return new FileItem()
                {
                    FileName = parts[0],
                    Extention = parts[1],
                    ExtentionLast = parts[2],
                    Size = long.Parse(parts[3]),
                    PackFileName = parts[4],
                    CheckSum = parts[5],
                    IsDb = IsDb(parts[0]),
                };
            }
            else if (version == 2)
            {
                return new FileItem()
                {
                    FileName = parts[0],
                    Extention = parts[1],
                    ExtentionLast = parts[2],
                    Size = long.Parse(parts[3]),
                    PackFileName = parts[4],
                    CheckSum = parts[5],
                    IsDb = IsDb(parts[0]),
                };
            }
            else if (version == 3)
            {
                return new FileItem()
                {
                    FileName = parts[0],
                    Extention = parts[1],
                    ExtentionLast = parts[2],
                    Size = long.Parse(parts[3]),
                    PackFileName = parts[4],
                    CheckSum = parts[5],
                    IsDb = bool.Parse(parts[6]),
                };
            }

            throw new Exception("Unknown version");
        }

        void WriteHeader(StreamWriter streamWriter)
        {
            streamWriter.WriteLine("sep=|");
            streamWriter.WriteLine("FileName|Extention|ExtentionLast|Size|PackFile|CheckSum|IsDb");
        }

        void WriteItem(StreamWriter writer, FileItem fileItem)
        {
            writer.WriteLine($"{fileItem.FileName}|{fileItem.Extention}|{fileItem.ExtentionLast}|{fileItem.Size}|{fileItem.PackFileName}|{fileItem.CheckSum}|{fileItem.IsDb}");
        }
    }
}
