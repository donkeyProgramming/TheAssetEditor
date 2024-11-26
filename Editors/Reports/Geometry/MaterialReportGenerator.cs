using System.Text;
using System.Windows;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.WsModel;

namespace Editors.Reports.Geometry
{
    public class MaterialReportCommand(MaterialReportGenerator generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class MaterialReportGenerator
    {
        private readonly IPackFileService _pfs;

        public MaterialReportGenerator(IPackFileService pfs)
        {
            _pfs = pfs;
        }

        public void Create()
        {
            var dirPath = $"{DirectoryHelper.ReportsDirectory}\\Material";
            DirectoryHelper.EnsureCreated(dirPath);

            var shaderMap = new Dictionary<string, List<WsModelMaterialFile>>();

            // Collect all files and sort them
            var fileList = PackFileServiceUtility.FindAllWithExtention(_pfs, ".wsmodel");
            var notFoundFiles = new List<string>();
            var errorFiles = new List<(string, Exception)>();
            foreach (var file in fileList)
            {
                try
                {
                    var wsModel = new WsModelFile(file);

                    foreach (var wsModelMaterialPath in wsModel.MaterialList)
                    {
                        var materialPackFile = _pfs.FindFile(wsModelMaterialPath.MaterialPath);
                        if (materialPackFile == null)
                        {
                            notFoundFiles.Add(wsModelMaterialPath.MaterialPath);
                            continue;
                        }
                        var material = new WsModelMaterialFile(materialPackFile);

                        if (shaderMap.ContainsKey(material.ShaderPath) == false)
                            shaderMap[material.ShaderPath] = new List<WsModelMaterialFile>();
                        shaderMap[material.ShaderPath].Add(material);
                    }
                }
                catch (Exception ex)
                {
                    errorFiles.Add((file.Name, ex));
                }
            }

            // Process output
            var failedShaders = new List<string>();
            foreach (var shaderCollection in shaderMap)
            {
                var path = shaderCollection.Key;
                var instances = shaderCollection.Value;

                foreach (var instance in instances)
                {
                    if (instance.Parameters.Count != instances.First().Parameters.Count)
                        failedShaders.Add(path + " - Unable to process - different number of parameters");
                }
                var sb = new StringBuilder();

                // Create header
                sb.AppendLine("Sep=|");
                sb.Append($"Name|");
                sb.Append(string.Join("|", instances.First().Parameters.OrderBy(x => x.Name).Select(x => x.Name)));
                sb.AppendLine();

                // Add instances
                foreach (var instance in instances)
                {
                    sb.Append(instance.Name + "|");
                    sb.Append(string.Join("|", instance.Parameters.OrderBy(x => x.Name).Select(x => x.Value)));
                    sb.AppendLine();
                }

                var outputFileName = path.Replace(".", "_").Replace("/", "_");
                File.WriteAllText(dirPath + "\\" + outputFileName + "_" + instances.Count + ".csv", sb.ToString());
            }

            // Create a summary
            var summaryOutput = new List<(string Shader, int UseCount, string Error, bool DifferentParameters)>();
            var summarySb = new StringBuilder();
            summarySb.AppendLine("Sep=|");
            summarySb.AppendLine("Shader|Instances|Error|DifferentParameters");

            foreach (var shaderCollection in shaderMap)
            {
                var path = shaderCollection.Key;
                var instances = shaderCollection.Value;
                var instanceCount = shaderCollection.Value.Count;

                if (path == "shaders/weighted2_daemon_prince_alpha.xml.shader")
                { }

                var error = "";
                foreach (var instance in instances)
                {
                    if (instance.Parameters.Count != instances.First().Parameters.Count)
                        error = "Unable to process - different number of parameters";
                }

                var differentParameters = false;
                foreach (var instance in instances)
                {
                    if (instance.Parameters.Count != instances.First().Parameters.Count)
                    {
                        differentParameters = true;
                        break;
                    }

                    var firstInstanceParams = instances.First().Parameters.OrderBy(x => x.Name).ToList();
                    var currentInstanceParams = instance.Parameters.OrderBy(x => x.Name).ToList();

                    for (var i = 0; i < firstInstanceParams.Count; i++)
                    {
                        if (firstInstanceParams[i].Value != currentInstanceParams[i].Value)
                        {
                            differentParameters = true;
                            break;
                        }
                    }
                }
                summarySb.AppendLine($"{path}|{instanceCount}|{error}|{differentParameters}");
            }

            File.WriteAllText(dirPath + "\\Summary.csv", summarySb.ToString());
            MessageBox.Show("Report completed");

        }
    }
}
