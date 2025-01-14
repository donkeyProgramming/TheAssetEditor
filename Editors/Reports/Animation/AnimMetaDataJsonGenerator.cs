using System.Diagnostics;
using System.Windows;
using Editors.AnimationTextEditors.AnimationPack.Converters;
using Newtonsoft.Json;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;

namespace Editors.Reports.Animation
{
    public class GenerateMetaJsonDataReportCommand(AnimMetaDataJsonGenerator generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class AnimMetaDataJsonGenerator
    {
        private readonly ILogger _logger = Logging.Create<AnimMetaDataJsonGenerator>();
        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _settingsService;
        private readonly MetaDataTagDeSerializer _metaDataTagDeSerializer;
        private readonly JsonSerializerSettings _jsonOptions;

        public AnimMetaDataJsonGenerator(IPackFileService pfs, ApplicationSettingsService settingsService, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _pfs = pfs;
            _settingsService = settingsService;
            _metaDataTagDeSerializer = metaDataTagDeSerializer;
            _jsonOptions = new JsonSerializerSettings { Formatting = Formatting.Indented };
        }

        public static void Generate(IPackFileService pfs, ApplicationSettingsService settingsService, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            var instance = new AnimMetaDataJsonGenerator(pfs, settingsService, metaDataTagDeSerializer);
            instance.Create();
        }

        void DumpAsJson(string gameOutputDir, string fileName, object data)
        {
            var jsonString = JsonConvert.SerializeObject(data, _jsonOptions);
            var jsonFilePath = Path.Join(gameOutputDir, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
            File.WriteAllText(jsonFilePath, jsonString);
        }

        public void Create()
        {
            var gameName = GameInformationDatabase.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var gameOutputDir = $"{DirectoryHelper.ReportsDirectory}\\MetaDataJsons\\{gameName}_{timeStamp}\\";
            if (Directory.Exists(gameOutputDir))
                Directory.Delete(gameOutputDir, true);
            DirectoryHelper.EnsureCreated(gameOutputDir);

            //dump animationTable
            var packFileContainer = _pfs.GetAllPackfileContainers();
            var animPack = packFileContainer[0].FileList["animations\\database\\battle\\bin\\animation_tables.animpack"];
            var animPackFile = AnimationPackSerializer.Load(animPack, _pfs);

            var converter = new AnimationBinWh3FileToXmlConverter(null, _metaDataTagDeSerializer);
            foreach (var animFile in animPackFile.Files)
            {
                if (animFile is AnimationBinWh3)
                {
                    var text = converter.GetText(animFile.ToByteArray());
                    var xmlFilePath = Path.Join(gameOutputDir, animFile.FileName + ".xml");
                    Directory.CreateDirectory(Path.GetDirectoryName(xmlFilePath));
                    File.WriteAllText(xmlFilePath, text);
                }
            }

            var allMeta = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_pfs, ".meta");
            foreach (var (fileName, packFile) in allMeta)
            {
                try
                {
                    var data = packFile.DataSource.ReadData();
                    if (data.Length == 0)
                        continue;

                    var parser = new MetaDataFileParser();
                    var metaData = parser.ParseFile(data, _metaDataTagDeSerializer);
                    DumpAsJson(gameOutputDir, fileName + ".json", metaData);
                }
                catch (Exception e)
                {
                    _logger.Here().Information($"Meta parsing failed {fileName} - {e.Message}");
                }
            }

            var allAnimations = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_pfs, ".anim");
            foreach (var (fileName, packFile) in allAnimations)
            {
                try
                {
                    var animationHeader = AnimationFile.GetAnimationHeader(packFile);
                    DumpAsJson(gameOutputDir, fileName + ".header.json", animationHeader);
                }
                catch (Exception e)
                {
                    _logger.Here().Information($"Animation parsing failed {fileName} - {e.Message}");
                }
            }

            MessageBox.Show($"Done - Created at {gameOutputDir}");
            Process.Start("explorer.exe", gameOutputDir);
        }
    }
}
