using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace AssetEditor.Report
{
    public class FileListReportGenerator
    {
        ILogger _logger = Logging.Create<FileListReportGenerator>();
        PackFileService _pfs;
        ApplicationSettingsService _settingsService;

        public FileListReportGenerator(PackFileService pfs, ApplicationSettingsService settingsService)
        {
            _pfs = pfs;
            _settingsService = settingsService;
        }

        public static void Generate(PackFileService pfs, ApplicationSettingsService settingsService)
        {
            var instance = new FileListReportGenerator(pfs, settingsService);
            instance.Create();
        }

        public void Create()
        {
            var outputFolder = DirectoryHelper.ReportsDirectory + "\\FileList";
            DirectoryHelper.EnsureCreated(outputFolder);
            var gameName = GameInformationFactory.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var outputFileName = $"{gameName} {DateTime.Now.ToString("yyyyMMddHHmmssfff")}.csv";
            var outputFilePath = $"{outputFolder}\\{outputFileName}";

            var containers = _pfs.GetAllPackfileContainers();
            var fileCount = containers.Sum(x => x.FileList.Count());
            _logger.Here().Information($"Creating file list report for {fileCount} files. Result will be saved at {outputFilePath}.)");

            var counter = 0;
            using var writer = new StreamWriter(outputFilePath, false);
            using var md5Instance = MD5.Create();

            writer.WriteLine("sep=|");
            writer.WriteLine("FileName|Extention|ExtentionLast|Size|PackFile|CheckSum");

            foreach (var container in containers)
            {
                foreach (var filePair in container.FileList)
                {
                    var fileName = filePair.Key;
                    var extention = Regex.Match(filePair.Key, @"\..*").Value;
                    var extentionLast = Path.GetExtension(fileName);
                    var size = filePair.Value.DataSource.Size;
                    var checkSum = md5Instance.ComputeHash(filePair.Value.DataSource.ReadData());
                    var checkSumStr = BitConverter.ToString(checkSum).Replace("-", "").ToLowerInvariant();
                    var packFileName = "";
                    if (filePair.Value.DataSource is PackedFileSource fileSource)
                        packFileName = fileSource.Parent.FilePath;
                    writer.WriteLine($"{fileName}|{extention}|{extentionLast}|{size}|{packFileName}|{checkSumStr}");

                    if (counter % 1000 == 0)
                        _logger.Here().Information($"Files processed {counter}/{fileCount} = {((float)counter / (float)fileCount) * 100}%");

                    counter++;
                }
            }

            MessageBox.Show($"Done - Created at {outputFilePath}");
            Process.Start("explorer.exe", outputFilePath);
        }
    }
}
