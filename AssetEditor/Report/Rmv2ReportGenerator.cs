using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.Services;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace AssetEditor.Report
{
    class Rmv2ReportGenerator
    {
        PackFileService _pfs;
        public Rmv2ReportGenerator(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public void Create(string gameDirectory, string outputDir = @"c:\temp\AssReports\rmv\")
        {
            var fileList = _pfs.FindAllWithExtention(".rigid_model_v2");

            var failedMeshRecords = new List<dynamic>();
            var versionInfoRecords = new List<dynamic>();
            var weightedMaterialRecords = new List<dynamic>();

            var modelFactory = ModelFactory.Create(false);
            for (int i = 0; i < fileList.Count; i++)
            {
                var meshFile = fileList[i];
                string path = _pfs.GetFullPath(meshFile);
                var rmvData = meshFile.DataSource.ReadData();
                try
                {
                    var rmvFile = modelFactory.Load(rmvData);

                    for (int lodIndex = 0; lodIndex < rmvFile.ModelList.Length; lodIndex++)
                    {
                        for (int modelIndex = 0; modelIndex < rmvFile.ModelList[lodIndex].Length; modelIndex++)
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
                                dynamic weightedMaterialRecord = new ExpandoObject();
                                weightedMaterialRecord.Path = path;

                                weightedMaterialRecord.Filters = weightedMaterial.Filters;
                                weightedMaterialRecord.MatrixIndex = weightedMaterial.MatrixIndex;
                                weightedMaterialRecord.ParentMatrixIndex = weightedMaterial.ParentMatrixIndex;
                                weightedMaterialRecord.AttachmentPointParams = weightedMaterial.AttachmentPointParams.Count;
                                weightedMaterialRecord.TexturesParams = weightedMaterial.TexturesParams.Count;
                                weightedMaterialRecord.StringParams = weightedMaterial.StringParams.Count;
                                weightedMaterialRecord.FloatParams = weightedMaterial.FloatParams.Count;
                                weightedMaterialRecord.IntParams = weightedMaterial.IntParams.Count;
                                weightedMaterialRecord.Vec4Params = weightedMaterial.Vec4Params.Count;

                                weightedMaterialRecords.Add(weightedMaterialRecord);
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

                CommonControls.Common.DirectoryHelper.EnsureCreated(outputDir);
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
