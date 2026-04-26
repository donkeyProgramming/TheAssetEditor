using System.Globalization;
using System.Text;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.Reports.Geometry
{
    public class RmvToTextCommand(RmvToTextReport report) : IRmvToTextCommand
    {
        public string GetDisplayName(TreeNode node) => "Generate Rmv to Text";

        public bool IsEnabled(TreeNode node) => node.NodeType == NodeType.File && node.Item != null && node.Name.EndsWith(".rigid_model_v2", StringComparison.OrdinalIgnoreCase);

        public void Execute(TreeNode node)
        {
            report.Generate(node.Item!);
        }
    }

    public class RmvToTextReport
    {
        private readonly IPackFileService _pfs;

        public RmvToTextReport(IPackFileService pfs)
        {
            _pfs = pfs;
        }

        public void Generate(PackFile packFile)
        {
            var fullPath = _pfs.GetFullPath(packFile);
            var fileName = Path.GetFileNameWithoutExtension(packFile.Name);
            var outputDir = Path.Combine(DirectoryHelper.ReportsDirectory, "RmvToFile");
            DirectoryHelper.EnsureCreated(outputDir);
            var outputPath = Path.Combine(outputDir, fileName + ".txt");

            var data = packFile.DataSource.ReadData();
            var modelFactory = ModelFactory.Create();
            var rmvFile = modelFactory.Load(data);

            var sb = new StringBuilder();

            WriteLodSummary(sb, rmvFile);
            WriteFileHeader(sb, rmvFile.Header);
            WriteLodHeaders(sb, rmvFile.LodHeaders, rmvFile.Header.LodCount);
            WriteAllLodModels(sb, rmvFile);

            File.WriteAllText(outputPath, sb.ToString());
            DirectoryHelper.OpenFolderAndSelectFile(outputPath);
        }

        private static void WriteLodSummary(StringBuilder sb, RmvFile rmvFile)
        {
            sb.AppendLine("=== LOD Summary ===");

            var totalPolygons = 0L;
            for (var lodIndex = 0; lodIndex < rmvFile.Header.LodCount; lodIndex++)
            {
                var models = rmvFile.ModelList[lodIndex];
                var meshCount = models.Length;
                var lodPolygons = models.Sum(m => (long)(m.CommonHeader.IndexCount / 3));
                totalPolygons += lodPolygons;
                sb.AppendLine($"LOD {lodIndex}: {meshCount} meshes, {lodPolygons} polygons");
            }

            sb.AppendLine($"Total polygons: {totalPolygons}");
            sb.AppendLine();
        }

        private static void WriteFileHeader(StringBuilder sb, RmvFileHeader header)
        {
            sb.AppendLine("=== File Header ===");
            sb.AppendLine($"FileType: {header.FileType}");
            sb.AppendLine($"Version: {header.Version}");
            sb.AppendLine($"LodCount: {header.LodCount}");
            sb.AppendLine($"SkeletonName: {header.SkeletonName}");
            sb.AppendLine();
        }

        private static void WriteLodHeaders(StringBuilder sb, RmvLodHeader[] lodHeaders, uint lodCount)
        {
            sb.AppendLine("=== LOD Headers ===");

            for (var i = 0; i < lodCount; i++)
            {
                var lod = lodHeaders[i];
                sb.AppendLine($"LOD {i}:");
                sb.AppendLine($"  MeshCount: {lod.MeshCount}");
                sb.AppendLine($"  TotalLodVertexSize: {lod.TotalLodVertexSize}");
                sb.AppendLine($"  TotalLodIndexSize: {lod.TotalLodIndexSize}");
                sb.AppendLine($"  FirstMeshOffset: {lod.FirstMeshOffset}");
                sb.AppendLine($"  QualityLvl: {lod.QualityLvl}");
                sb.AppendLine($"  LodCameraDistance: {F(lod.LodCameraDistance)}");
            }

            sb.AppendLine();
        }

        private static void WriteAllLodModels(StringBuilder sb, RmvFile rmvFile)
        {
            if (rmvFile.Header.LodCount == 0)
                return;

            sb.AppendLine("=== Models By LOD ===");

            for (var lodIndex = 0; lodIndex < rmvFile.Header.LodCount; lodIndex++)
            {
                var models = rmvFile.ModelList[lodIndex];
                sb.AppendLine($"=== LOD {lodIndex} Models ({models.Length}) ===");

                for (var modelIndex = 0; modelIndex < models.Length; modelIndex++)
                {
                    var model = models[modelIndex];
                    sb.AppendLine($"--- LOD {lodIndex} Model {modelIndex} ---");
                    sb.AppendLine($"ModelTypeFlag: {model.CommonHeader.ModelTypeFlag}");
                    sb.AppendLine($"RenderFlag: {model.CommonHeader.RenderFlag}");
                    sb.AppendLine($"VertexCount: {model.CommonHeader.VertexCount}");
                    sb.AppendLine($"IndexCount: {model.CommonHeader.IndexCount}");
                    WriteMaterial(sb, model.Material);
                    sb.AppendLine("Vertices:");

                    var vertices = model.Mesh.VertexList;
                    for (var vi = 0; vi < vertices.Length; vi++)
                    {
                        var v = vertices[vi];
                        sb.Append($"  VertexId={vi}");
                        sb.Append($" | Pos({F(v.Position.X)}, {F(v.Position.Y)}, {F(v.Position.Z)}, {F(v.Position.W)})");
                        sb.Append($" | N({F(v.Normal.X)}, {F(v.Normal.Y)}, {F(v.Normal.Z)})");
                        sb.Append($" | BiN({F(v.BiNormal.X)}, {F(v.BiNormal.Y)}, {F(v.BiNormal.Z)})");
                        sb.Append($" | Tan({F(v.Tangent.X)}, {F(v.Tangent.Y)}, {F(v.Tangent.Z)})");
                        sb.Append($" | UV0({F(v.Uv.X)}, {F(v.Uv.Y)})");
                        sb.Append($" | UV1({F(v.Uv1.X)}, {F(v.Uv1.Y)})");
                        sb.Append($" | Col({F(v.Colour.X)}, {F(v.Colour.Y)}, {F(v.Colour.Z)}, {F(v.Colour.W)})");
                        sb.Append($" | BoneIdx({FormatBoneIndices(v.BoneIndex)})");
                        sb.Append($" | BoneW({FormatBoneWeights(v.BoneWeight)})");
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }
            }
        }

        private static void WriteMaterial(StringBuilder sb, IRmvMaterial material)
        {
            sb.AppendLine("Material:");
            sb.AppendLine($"  MaterialId: {material.MaterialId}");
            sb.AppendLine($"  BinaryVertexFormat: {material.BinaryVertexFormat}");
            sb.AppendLine($"  ModelName: {material.ModelName}");
            sb.AppendLine($"  TextureDirectory: {material.TextureDirectory}");
            sb.AppendLine($"  PivotPoint: ({F(material.PivotPoint.X)}, {F(material.PivotPoint.Y)}, {F(material.PivotPoint.Z)})");

            sb.AppendLine("  Textures:");
            foreach (var texture in material.GetAllTextures())
                sb.AppendLine($"    {texture.TexureType}: {texture.Path}");

            if (material is WeightedMaterial weighted)
            {
                sb.AppendLine($"  Filters: {weighted.Filters}");
                sb.AppendLine($"  MatrixIndex: {weighted.MatrixIndex}");
                sb.AppendLine($"  ParentMatrixIndex: {weighted.ParentMatrixIndex}");
                sb.AppendLine($"  MaterialHint: {weighted.MaterialHint}");
                sb.AppendLine($"  ToolVertexFormat: {weighted.ToolVertexFormat}");
                sb.AppendLine($"  AttachmentPoints: {weighted.AttachmentPointParams.Count}");
                foreach (var ap in weighted.AttachmentPointParams)
                    sb.AppendLine($"    {ap.Name} (BoneIndex: {ap.BoneIndex})");
                sb.AppendLine($"  StringParams: {weighted.StringParams.Values.Count}");
                sb.AppendLine($"  FloatParams: {weighted.FloatParams.Values.Count}");
                sb.AppendLine($"  IntParams: {weighted.IntParams.Values.Count}");
                sb.AppendLine($"  Vec4Params: {weighted.Vec4Params.Values.Count}");
            }
        }

        private static string F(float value) => value.ToString("F6", CultureInfo.InvariantCulture);

        private static string FormatBoneIndices(byte[] indices)
        {
            if (indices == null || indices.Length == 0)
                return "-";
            return string.Join(", ", indices);
        }

        private static string FormatBoneWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return "-";
            return string.Join(", ", weights.Select(w => F(w)));
        }
    }
}
