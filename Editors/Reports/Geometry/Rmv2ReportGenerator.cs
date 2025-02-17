using System.Dynamic;
using System.Globalization;
using System.Windows;
using CsvHelper;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Editors.Reports.Geometry
{
    public class Rmv2ReportCommand(Rmv2ReportGenerator generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class Rmv2ReportGenerator
    {
        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public Rmv2ReportGenerator(IPackFileService pfs, ApplicationSettingsService applicationSettingsService)
        {
            _pfs = pfs;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Create(string outputDir = @"c:\temp\AssReports\rmv\")
        {
            var gameName = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).DisplayName;
            var gameDirectory = gameName;
            
            var fileList = PackFileServiceUtility.FindAllWithExtention(_pfs, ".rigid_model_v2");

            var failedMeshRecords = new List<dynamic>();
            var versionInfoRecords = new List<dynamic>();
            var weightedMaterialRecords = new List<dynamic>();

            var modelFactory = ModelFactory.Create();
            for (var i = 0; i < fileList.Count; i++)
            {
                var meshFile = fileList[i];
                var path = _pfs.GetFullPath(meshFile);
                if (path.Contains("terrain\\tiles"))
                    continue;
                var rmvData = meshFile.DataSource.ReadData();
                try
                {
                    var rmvFile = modelFactory.Load(rmvData);

                    for (var lodIndex = 0; lodIndex < rmvFile.ModelList.Length; lodIndex++)
                    {
                        for (var modelIndex = 0; modelIndex < rmvFile.ModelList[lodIndex].Length; modelIndex++)
                        {
                            var model = rmvFile.ModelList[lodIndex][modelIndex];
                            dynamic versionInfoRecord = new ExpandoObject();

                            versionInfoRecord.Path = path;
                            versionInfoRecord.RmvVersion = rmvFile.Header.Version;
                            versionInfoRecord.MaterialType = model.CommonHeader.ModelTypeFlag;
                            versionInfoRecord.RenderFlag = model.CommonHeader.RenderFlag;
                            versionInfoRecord.VertexType = model.Material.BinaryVertexFormat;
                            versionInfoRecords.Add(versionInfoRecord);

                            if (model.Material is WeightedMaterial weightedMaterial)
                            {
                                // dynamic weightedMaterialRecord = new ExpandoObject();
                                // weightedMaterialRecord.Path = path;
                                //
                                // weightedMaterialRecord.Filters = weightedMaterial.Filters;
                                // weightedMaterialRecord.MatrixIndex = weightedMaterial.MatrixIndex;
                                // weightedMaterialRecord.ParentMatrixIndex = weightedMaterial.ParentMatrixIndex;
                                // weightedMaterialRecord.AttachmentPointParams = weightedMaterial.AttachmentPointParams.Count;
                                // weightedMaterialRecord.TexturesParams = weightedMaterial.TexturesParams.Count;
                                // weightedMaterialRecord.StringParams = weightedMaterial.StringParams.Count;
                                // weightedMaterialRecord.FloatParams = weightedMaterial.FloatParams.Count;
                                // weightedMaterialRecord.IntParams = weightedMaterial.IntParams.Count;
                                // weightedMaterialRecord.Vec4Params = weightedMaterial.Vec4Params.Count;
                                //
                                // weightedMaterialRecords.Add(weightedMaterialRecord);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    dynamic failedRecord = new ExpandoObject();
                    failedRecord.Path = path;
                    failedRecord.Error = ExceptionHelper.GetErrorString(e, " - ");
                    failedMeshRecords.Add(failedRecord);
                }
            }

            try
            {
                var fullOutputDir = outputDir + gameDirectory + "\\";

                DirectoryHelper.EnsureCreated(outputDir);
                Write(failedMeshRecords, outputDir + "LoadResult.csv");
                Write(versionInfoRecords, outputDir + "VersionAndMaterialInfo.csv");
                Write(weightedMaterialRecords, outputDir + "WeightedMaterialInfo.csv");

                MessageBox.Show($"Done - Created at {outputDir}");
            }
            catch
            {
                MessageBox.Show("Unable to write reports to file!");
            }
        }

        void Write(List<dynamic> dataRecords, string filePath)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(dataRecords);
            File.WriteAllText(filePath, writer.ToString());
        }
    }
}
