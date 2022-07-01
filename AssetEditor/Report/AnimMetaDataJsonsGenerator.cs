using CommonControls.Common;
using CommonControls.FileTypes.MetaData;
using CommonControls.Services;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Windows;
using CommonControls.Editors.AnimationPack.Converters;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3;
using CommonControls.FileTypes.PackFiles.Models;

namespace AssetEditor.Report
{
    class AnimMetaDataJsonsGenerator
    {
        ILogger _logger = Logging.Create<AnimMetaDataJsonsGenerator>();
        PackFileService _pfs;
        ApplicationSettingsService _settingsService;
        public AnimMetaDataJsonsGenerator(PackFileService pfs, ApplicationSettingsService settingsService)
        {
            _pfs = pfs;
            _settingsService = settingsService;
        }

        public static void Generate(PackFileService pfs, ApplicationSettingsService settingsService)
        {
            var instance = new AnimMetaDataJsonsGenerator(pfs, settingsService);
            instance.Create();
        }

        public void Create()
        {
            var gameName = GameInformationFactory.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var gameOutputDir = $"{DirectoryHelper.ReportsDirectory}\\MetaDataJsons\\{gameName}_{timeStamp}\\";
            if (Directory.Exists(gameOutputDir))
                Directory.Delete(gameOutputDir, true);
            DirectoryHelper.EnsureCreated(gameOutputDir);
            
            //dump animtable
            PackFile animPack = _pfs.Database.PackFiles[0].FileList["animations\\database\\battle\\bin\\animation_tables.animpack"];
            AnimationPackFile animPackFile = AnimationPackSerializer.Load(animPack, _pfs);

            AnimationBinWh3FileToXmlConverter converter = new AnimationBinWh3FileToXmlConverter(new SkeletonAnimationLookUpHelper());
            foreach (var animFile in animPackFile.Files)
            {
                if (animFile is AnimationBinWh3)
                {
                    string text = converter.GetText(animFile.ToByteArray());
                    string xml_filepath = Path.Join(gameOutputDir, animFile.FileName + ".xml");
                    Directory.CreateDirectory(Path.GetDirectoryName(xml_filepath));
                    File.WriteAllText(xml_filepath, text);
                }
            }

            var fileList = _pfs.FindAllWithExtentionIncludePaths(".meta");
            for (int i = 0; i < fileList.Count; i++)
            {
                var fileName = fileList[i].FileName;
                var packFile = fileList[i].Pack;

                if (fileName.Contains(".snd."))
                {
                    continue;
                }
                // _logger.Here().Information($"Parsing {fileName}");

                try
                {
                    var data = packFile.DataSource.ReadData(); 
                    if (data.Length == 0)
                        continue;

                    var parser = new MetaDataFileParser();
                    var metaData = parser.ParseFile(data);
  
                    var options = new JsonSerializerSettings { Formatting = Formatting.Indented };
                    string jsonString = JsonConvert.SerializeObject(metaData, options);
                    string json_filepath = Path.Join(gameOutputDir, fileName + ".json");
                    Directory.CreateDirectory(Path.GetDirectoryName(json_filepath));
                    File.WriteAllText(json_filepath, jsonString);
                }
                catch(Exception e)
                {
                    _logger.Here().Information($"Parsing failed {fileName} - {e.Message}");
                }
            }
            MessageBox.Show($"Done - Created at {gameOutputDir}");
            Process.Start("explorer.exe", gameOutputDir);
        }
    }
}
