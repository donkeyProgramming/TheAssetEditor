using System.Drawing;
using System.IO;
using Editors.ImportExport.Misc;
using MeshImportExport;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.ImportExport.Exporting.Exporters.DdsToNormalPng
{

    public interface IDdsToNormalPngExporter
    {
        public string Export(string filePath, string outputPath, bool convertToBlueNormalMap);
        public ExportSupportEnum CanExportFile(PackFile file);
    }

    public class DdsToNormalPngExporter : IDdsToNormalPngExporter
    {
        private readonly IPackFileService _pfs;
        private readonly IImageSaveHandler _imageSaveHandler;

        public DdsToNormalPngExporter(IPackFileService packFileService, IImageSaveHandler imageSaveHandler) 
        {
            _pfs = packFileService;
            _imageSaveHandler = imageSaveHandler;
        }

        public ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsDdsMaterialFile(file.Name))
                return ExportSupportEnum.HighPriority;
            else if (FileExtensionHelper.IsDdsFile(file.Name))
                return ExportSupportEnum.Supported;
            return ExportSupportEnum.NotSupported;
        }

        public string Export(string filePath, string outputPath, bool convertToBlueNormalMap)
        {
            var packFile = _pfs.FindFile(filePath);
            if (packFile == null)
                return "";

            var fileName = Path.GetFileNameWithoutExtension(
                filePath.Replace('\\', Path.DirectorySeparatorChar));
            var outDirectory = Path.GetDirectoryName(outputPath) ?? string.Empty;
            var outputFilePath = Path.Combine(
                outDirectory,
                convertToBlueNormalMap ? fileName + ".png" : fileName + "_raw.png");

            var bytes = packFile.DataSource.ReadData();
            if (bytes == null || !bytes.Any())
                throw new Exception($"Could not read file data. bytes.Count = {bytes?.Length}");

            var imgBytes = TextureHelper.ConvertDdsToPng(bytes);
            if (imgBytes == null || !imgBytes.Any())
                throw new Exception($"image data invalid/empty. imgBytes.Count = {imgBytes?.Length}");

            if (convertToBlueNormalMap)
                imgBytes = ConvertPackedNormalToStandard(imgBytes);

            _imageSaveHandler.Save(imgBytes, outputFilePath);

            return outputFilePath;
        }

        private static byte[] ConvertPackedNormalToStandard(byte[] pngBytes)
        {
            using var inputStream = new MemoryStream(pngBytes);
            using var image = Image.FromStream(inputStream);
            using var source = new Bitmap(image);
            var sourcePixels = BitmapPixelBuffer.ReadBgra(source);
            var outputPixels = new byte[sourcePixels.Length];

            for (var index = 0; index < sourcePixels.Length; index += 4)
            {
                var packedR = sourcePixels[index + 2];
                var packedG = sourcePixels[index + 1];
                var packedA = sourcePixels[index + 3];

                // WH3 stores tangent-space X as R*A and Y as G. Keep the
                // shader's Y orientation; only reconstruct Z and emit a
                // conventional opaque RGB normal texture.
                var x01 = (packedR / 255d) * (packedA / 255d);
                var y01 = packedG / 255d;
                var normalX = Math.Clamp(2d * x01 - 1d, -1d, 1d);
                var normalY = Math.Clamp(2d * y01 - 1d, -1d, 1d);
                var normalZ = Math.Sqrt(Math.Max(0d, 1d - normalX * normalX - normalY * normalY));

                outputPixels[index] = EncodeNormalComponent(normalZ);
                outputPixels[index + 1] = EncodeNormalComponent(normalY);
                outputPixels[index + 2] = EncodeNormalComponent(normalX);
                outputPixels[index + 3] = 255;
            }

            using var output = BitmapPixelBuffer.CreateBitmap(source.Width, source.Height, outputPixels);
            using var outputStream = new MemoryStream();
            output.Save(outputStream, System.Drawing.Imaging.ImageFormat.Png);
            return outputStream.ToArray();
        }

        private static byte EncodeNormalComponent(double component)
        {
            if (!double.IsFinite(component))
                return 0;

            var encoded = ((Math.Clamp(component, -1d, 1d) + 1d) * 0.5d) * 255d;
            return (byte)Math.Clamp((int)Math.Round(encoded, MidpointRounding.AwayFromZero), 0, 255);
        }
    }
}
