using System.IO;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.Core.PackFiles;
using SharpGLTF.Materials;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    public record TextureResult(int MeshIndex, string SystemFilePath, KnownChannel GlftTexureType, bool HasAlphaChannel = false);
    public record MaskTextureResult(int MeshIndex, string SystemFilePath);

    public interface IGltfTextureHandler
    {
        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings);
    }

    public class GltfTextureHandler : IGltfTextureHandler
    {
        private readonly IDdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly IDdsToMaterialPngExporter _ddsToMaterialPngExporter;
        private readonly IPackFileService _packFileService;

        public GltfTextureHandler(IDdsToNormalPngExporter ddsToNormalPngExporter, IDdsToMaterialPngExporter ddsToMaterialPngExporter, IPackFileService packFileService = null)
        {
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;

            _packFileService = packFileService;
        }

        public List<TextureResult> HandleTextures(RmvFile rmvFile, RmvToGltfExporterSettings settings)
        {
            var output = new List<TextureResult>();

            if (!settings.ExportMaterials)
                return output;

            var exportedTextures = new Dictionary<string, string>();    // To avoid exporting same texture multiple times

            int lodICounnt = 1;
            for (var lodIndex = 0; lodIndex < lodICounnt; lodIndex++)
            {
                for (var meshIndex = 0; meshIndex < rmvFile.ModelList[lodIndex].Length; meshIndex++)
                {
                    var model = rmvFile.ModelList[lodIndex][meshIndex];
                    var textures = ExtractTextures(model);

                    foreach (var tex in textures)
                    {
                        switch (tex.Type)
                        {
                            case TextureType.Normal: DoTextureConversionNormalMap(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.MaterialMap: DoTextureConversionMaterialMap(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.BaseColour: 
                            case TextureType.Diffuse: 
                                DoTextureDefault(KnownChannel.BaseColor, settings, output, exportedTextures, meshIndex, tex);
                                break;
                            case TextureType.Mask: DoTextureMask(settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Specular: DoTextureDefault(KnownChannel.SpecularColor, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Gloss: DoTextureDefault(KnownChannel.MetallicRoughness, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Ambient_occlusion: DoTextureDefault(KnownChannel.Occlusion, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.Emissive: DoTextureDefault(KnownChannel.Emissive, settings, output, exportedTextures, meshIndex, tex); break;
                            case TextureType.EmissiveDistortion: DoTextureDefault(KnownChannel.Emissive, settings, output, exportedTextures, meshIndex, tex); break;
                        }
                    }
                }


            }

            return output;
        }
        interface IDDsToPngExporter
        {
            public string Export(string path, string outputPath, bool convertToBlender)
            {
                throw new System.NotImplementedException();
            }
        }


        List<MaterialBuilderTextureInput> ExtractTextures(RmvModel model)
        {
            var textures = model.Material.GetAllTextures();
            var output = textures.Select(x => new MaterialBuilderTextureInput(x.Path, x.TexureType)).ToList();
            return output;
        }

        record MaterialBuilderTextureInput(string Path, TextureType Type);

        private void DoTextureConversionMaterialMap(RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
                exportedTextures[text.Path] = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, KnownChannel.MetallicRoughness));
        }

        private void DoTextureDefault(KnownChannel textureType, RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
                exportedTextures[text.Path] = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, false);

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, textureType, false));
        }

        private void DoTextureMask(RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
            {
                // Export mask as separate PNG - name it with _mask suffix for clarity
                var exportedPath = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, false);

                if (exportedPath != null)
                {
                    // Invert the mask values for proper alpha channel usage
                    // Game masks are often inverted (black=show, white=hide)
                    // Alpha channels need (black=transparent, white=opaque)
                    InvertMaskImage(exportedPath);

                    // Rename to have _mask suffix
                    var directory = Path.GetDirectoryName(exportedPath);
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(exportedPath);
                    var newFileName = fileNameWithoutExt + "_mask.png";
                    var newPath = Path.Combine(directory, newFileName);

                    if (File.Exists(exportedPath))
                    {
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(exportedPath, newPath);
                        exportedPath = newPath;
                    }
                }

                exportedTextures[text.Path] = exportedPath;
            }

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
            {
                // Export mask as a regular texture - user will connect it manually in Blender
                // We'll add it as a separate texture that doesn't get auto-connected but is available
                output.Add(new TextureResult(meshIndex, systemPath, KnownChannel.BaseColor, false));
            }
        }

        private void InvertMaskImage(string imagePath)
        {
            byte[] imageBytes;

            // Load image into memory to avoid file lock
            using (var fs = File.OpenRead(imagePath))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                imageBytes = ms.ToArray();
            }

            // Process the image from memory
            using var imageStream = new MemoryStream(imageBytes);
            using var image = System.Drawing.Image.FromStream(imageStream);
            using var bitmap = new System.Drawing.Bitmap(image);

            // Invert all pixel values (255 - value)
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    var invertedR = 255 - pixel.R;
                    var invertedG = 255 - pixel.G;
                    var invertedB = 255 - pixel.B;
                    var invertedColor = System.Drawing.Color.FromArgb(pixel.A, invertedR, invertedG, invertedB);
                    bitmap.SetPixel(x, y, invertedColor);
                }
            }

            // Save back to the same file
            bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void DoTextureConversionNormalMap(RmvToGltfExporterSettings settings, List<TextureResult> output, Dictionary<string, string> exportedTextures, int meshIndex, MaterialBuilderTextureInput text)
        {
            if (exportedTextures.ContainsKey(text.Path) == false)
            {
                exportedTextures[text.Path] = _ddsToNormalPngExporter.Export(text.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);

                ExportNormalMapVariants(text.Path, settings.OutputPath);
            }

            var systemPath = exportedTextures[text.Path];
            if (systemPath != null)
                output.Add(new TextureResult(meshIndex, systemPath, KnownChannel.Normal));
        }

        private void ExportNormalMapVariants(string packFilePath, string outputPath)
        {
            if (_packFileService == null)
                return;

            var packFile = _packFileService.FindFile(packFilePath);
            if (packFile == null)
                return;

            var fileName = Path.GetFileNameWithoutExtension(packFilePath);
            var outDirectory = Path.GetDirectoryName(outputPath);

            var bytes = packFile.DataSource.ReadData();
            if (bytes != null && bytes.Any())
            {
                ExportRawNormalMapPng(bytes, outDirectory, fileName);
                ExportOffsetNormalMapPng(bytes, outDirectory, fileName);
                ExportDisplacementMapPng(bytes, outDirectory, fileName);
            }
        }

        private void ExportRawNormalMapPng(byte[] ddsBytes, string outDirectory, string fileName)
        {
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);
            w.Write(ddsBytes);
            m.Seek(0, SeekOrigin.Begin);

            var image = Pfim.Pfimage.FromStream(m);

            var pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            if (image.Format == Pfim.ImageFormat.Rgba32)
            {
                pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            }
            else if (image.Format == Pfim.ImageFormat.Rgb24)
            {
                pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            }
            else
            {
                return;
            }

            using var tempBitmap = new System.Drawing.Bitmap(image.Width, image.Height, pixelFormat);

            var bitmapData = tempBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                pixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bitmapData.Scan0, image.DataLen);
            tempBitmap.UnlockBits(bitmapData);

            using var decodedBitmap = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < tempBitmap.Height; y++)
            {
                for (int x = 0; x < tempBitmap.Width; x++)
                {
                    var pixel = tempBitmap.GetPixel(x, y);

                    float r = pixel.R / 255.0f;
                    float g = pixel.G / 255.0f;
                    float a = pixel.A / 255.0f;

                    float decodedX = r * a;
                    float decodedY = g;

                    decodedX = decodedX * 2.0f - 1.0f;
                    decodedY = decodedY * 2.0f - 1.0f;

                    float decodedZ = (float)Math.Sqrt(Math.Max(0, 1.0f - decodedX * decodedX - decodedY * decodedY));

                    byte finalR = (byte)((decodedX + 1.0f) * 0.5f * 255.0f);
                    byte finalG = (byte)((decodedY + 1.0f) * 0.5f * 255.0f);
                    byte finalB = (byte)((decodedZ + 1.0f) * 0.5f * 255.0f);
                    byte finalA = (byte)(a * 255.0f);

                    decodedBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(finalA, finalR, finalG, finalB));
                }
            }

            var rawPngPath = Path.Combine(outDirectory, fileName + "_raw.png");
            decodedBitmap.Save(rawPngPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ExportOffsetNormalMapPng(byte[] ddsBytes, string outDirectory, string fileName)
        {
            var rawPngPath = Path.Combine(outDirectory, fileName + "_raw.png");

            if (!File.Exists(rawPngPath))
                return;

            using var rawImage = System.Drawing.Image.FromFile(rawPngPath);
            using var outputBitmap = new System.Drawing.Bitmap(rawImage.Width, rawImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = System.Drawing.Graphics.FromImage(outputBitmap);

            graphics.Clear(System.Drawing.Color.FromArgb(255, 128, 128, 255));
            graphics.DrawImage(rawImage, 0, 0);

            var offsetPngPath = Path.Combine(outDirectory, fileName + "_offset.png");
            outputBitmap.Save(offsetPngPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ExportDisplacementMapPng(byte[] ddsBytes, string outDirectory, string fileName)
        {
            var rawPngPath = Path.Combine(outDirectory, fileName + "_raw.png");

            if (!File.Exists(rawPngPath))
                return;

            using var rawImage = System.Drawing.Image.FromFile(rawPngPath);
            using var rawBitmap = new System.Drawing.Bitmap(rawImage);

            int width = rawBitmap.Width;
            int height = rawBitmap.Height;

            // Convert normal map to initial grayscale using luminance
            float[,] heightMap = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = rawBitmap.GetPixel(x, y);
                    // Use standard luminance weights
                    float gray = (pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f) / 255.0f;
                    heightMap[x, y] = gray;
                }
            }

            // Apply iterative smoothing (relaxation) to generate height from normals
            // This mimics the shader's multi-pass approach
            const int iterations = 10;
            float[,] tempMap = new float[width, height];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = 0;
                        int count = 0;

                        // Sample neighbors (above, left, right, below)
                        if (y > 0) { sum += heightMap[x, y - 1]; count++; }
                        if (x > 0) { sum += heightMap[x - 1, y]; count++; }
                        if (x < width - 1) { sum += heightMap[x + 1, y]; count++; }
                        if (y < height - 1) { sum += heightMap[x, y + 1]; count++; }

                        tempMap[x, y] = count > 0 ? sum / count : heightMap[x, y];
                    }
                }

                // Swap buffers
                Array.Copy(tempMap, heightMap, width * height);
            }

            // Apply contrast adjustment (factor 0.1)
            const float contrastFactor = 0.1f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = heightMap[x, y];
                    heightMap[x, y] = Math.Clamp((value - 0.5f) * (1.0f + contrastFactor) + 0.5f, 0, 1);
                }
            }

            // Apply sharpening filter (strength 1)
            const float sharpenStrength = 1.0f;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float center = heightMap[x, y];
                    float top = heightMap[x, y - 1];
                    float bottom = heightMap[x, y + 1];
                    float left = heightMap[x - 1, y];
                    float right = heightMap[x + 1, y];

                    float sharpened = center * (1.0f + 4.0f * sharpenStrength) 
                                    - (top + bottom + left + right) * sharpenStrength;

                    tempMap[x, y] = Math.Clamp(sharpened, 0, 1);
                }
            }

            // Copy sharpened values back (skip edges)
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    heightMap[x, y] = tempMap[x, y];
                }
            }

            // Normalize to 0-255 range
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    minHeight = Math.Min(minHeight, heightMap[x, y]);
                    maxHeight = Math.Max(maxHeight, heightMap[x, y]);
                }
            }

            using var displacementBitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedHeight;
                    if (maxHeight > minHeight)
                    {
                        normalizedHeight = (heightMap[x, y] - minHeight) / (maxHeight - minHeight);
                    }
                    else
                    {
                        normalizedHeight = 0.5f;
                    }

                    byte grayscale = (byte)Math.Clamp(normalizedHeight * 255.0f, 0, 255);

                    displacementBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, grayscale, grayscale, grayscale));
                }
            }

            var displacementPngPath = Path.Combine(outDirectory, fileName + "_displacement.png");
            displacementBitmap.Save(displacementPngPath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
