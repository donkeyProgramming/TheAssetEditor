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
            {
                exportedTextures[text.Path] = _ddsToMaterialPngExporter.Export(text.Path, settings.OutputPath, false);

                // For 3D printing: Export alpha channel as a separate mask for base color/diffuse
                if (settings.ExportDisplacementMaps && textureType == KnownChannel.BaseColor)
                {
                    ExportAlphaMask(text.Path, settings.OutputPath);
                }
            }

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
                // Only export displacement maps for 3D printing workflow
                if (settings.ExportDisplacementMaps)
                {
                    // Export normal map variants with proper YCoCg decoding
                    ExportNormalMapVariants(text.Path, settings.OutputPath);
                    ExportDisplacementFromNormalMap(text.Path, settings.OutputPath, settings);

                    // Set the path to the raw normal map
                    var fileName = Path.GetFileNameWithoutExtension(text.Path);
                    var outDirectory = Path.GetDirectoryName(settings.OutputPath);
                    exportedTextures[text.Path] = Path.Combine(outDirectory, fileName + "_raw.png");
                }
                else
                {
                    // Regular export: use the standard DDS to PNG exporter
                    exportedTextures[text.Path] = _ddsToNormalPngExporter.Export(text.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);
                }
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
            }
        }

        private void ExportAlphaMask(string packFilePath, string outputPath)
        {
            if (_packFileService == null)
                return;

            var packFile = _packFileService.FindFile(packFilePath);
            if (packFile == null)
                return;

            var fileName = Path.GetFileNameWithoutExtension(packFilePath);
            var outDirectory = Path.GetDirectoryName(outputPath);

            var bytes = packFile.DataSource.ReadData();
            if (bytes == null || !bytes.Any())
                return;

            // Convert DDS to bitmap
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);
            w.Write(bytes);
            m.Seek(0, SeekOrigin.Begin);

            var image = Pfim.Pfimage.FromStream(m);

            if (image.Format != Pfim.ImageFormat.Rgba32)
                return; // No alpha channel

            using var sourceBitmap = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bitmapData = sourceBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bitmapData.Scan0, image.DataLen);
            sourceBitmap.UnlockBits(bitmapData);

            // Extract alpha channel as black and white mask
            using var maskBitmap = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = sourceBitmap.GetPixel(x, y);
                    byte alpha = pixel.A;

                    // Create grayscale mask from alpha channel
                    // White = opaque (alpha 255), Black = transparent (alpha 0)
                    maskBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, alpha, alpha, alpha));
                }
            }

            var maskPath = Path.Combine(outDirectory, fileName + "_alphamask.png");
            maskBitmap.Save(maskPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void ExportDisplacementFromNormalMap(string normalMapPath, string outputPath, RmvToGltfExporterSettings settings)
        {
            var fileName = Path.GetFileNameWithoutExtension(normalMapPath);
            var outDirectory = Path.GetDirectoryName(outputPath);

            if (_packFileService == null)
                return;

            var packFile = _packFileService.FindFile(normalMapPath);
            if (packFile == null)
                return;

            var bytes = packFile.DataSource.ReadData();
            if (bytes != null && bytes.Any())
            {
                ExportDisplacementMapPng(bytes, outDirectory, fileName, settings);
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

            using var rawBitmap = new System.Drawing.Bitmap(image.Width, image.Height, pixelFormat);

            var bitmapData = rawBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                pixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bitmapData.Scan0, image.DataLen);
            rawBitmap.UnlockBits(bitmapData);

            var rawPngPath = Path.Combine(outDirectory, fileName + "_raw.png");
            rawBitmap.Save(rawPngPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ExportOffsetNormalMapPng(byte[] ddsBytes, string outDirectory, string fileName)
        {
            var rawPngPath = Path.Combine(outDirectory, fileName + "_raw.png");

            if (!File.Exists(rawPngPath))
                return;

            using var rawImage = System.Drawing.Image.FromFile(rawPngPath);
            using var rawBitmap = new System.Drawing.Bitmap(rawImage);
            using var outputBitmap = new System.Drawing.Bitmap(rawBitmap.Width, rawBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            const int bgR = 128;
            const int bgG = 128;
            const int bgB = 255;

            // Manually composite pixel-by-pixel for proper alpha blending
            for (int y = 0; y < rawBitmap.Height; y++)
            {
                for (int x = 0; x < rawBitmap.Width; x++)
                {
                    var pixel = rawBitmap.GetPixel(x, y);

                    // Note: Swap R and B because raw PNG is in BGRA format from Pfim
                    float alpha = pixel.A / 255.0f;
                    float invAlpha = 1.0f - alpha;

                    int compositeR = (int)(pixel.B * alpha + bgR * invAlpha); // Use B for R
                    int compositeG = (int)(pixel.G * alpha + bgG * invAlpha);
                    int compositeB = (int)(pixel.R * alpha + bgB * invAlpha); // Use R for B

                    compositeR = Math.Clamp(compositeR, 0, 255);
                    compositeG = Math.Clamp(compositeG, 0, 255);
                    compositeB = Math.Clamp(compositeB, 0, 255);

                    outputBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, compositeR, compositeG, compositeB));
                }
            }

            var offsetPngPath = Path.Combine(outDirectory, fileName + "_offset.png");
            outputBitmap.Save(offsetPngPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ExportDisplacementMapPng(byte[] ddsBytes, string outDirectory, string fileName, RmvToGltfExporterSettings settings)
        {
            var offsetPngPath = Path.Combine(outDirectory, fileName + "_offset.png");

            if (!File.Exists(offsetPngPath))
                return;

            using var offsetImage = System.Drawing.Image.FromFile(offsetPngPath);
            using var offsetBitmap = new System.Drawing.Bitmap(offsetImage);

            int width = offsetBitmap.Width;
            int height = offsetBitmap.Height;

            // Export standard displacement map (luminance + smoothing)
            var standardHeightMap = StandardHeightMapGeneration(offsetBitmap, settings.DisplacementIterations);
            ApplyContrast(standardHeightMap, settings.DisplacementContrast);

            if (settings.DisplacementSharpness > 0)
            {
                standardHeightMap = ApplyBilateralFilter(standardHeightMap, settings.DisplacementSharpness);
            }

            NormalizeHeightMap(standardHeightMap, out float minHeight, out float maxHeight);

            if (settings.Export16BitDisplacement)
            {
                Save16BitDisplacementMap(standardHeightMap, outDirectory, fileName);
            }
            else
            {
                Save8BitDisplacementMap(standardHeightMap, outDirectory, fileName + "_displacement");
            }

            // Export Poisson reconstruction version for comparison (if enabled)
            if (settings.UsePoissonReconstruction)
            {
                var poissonHeightMap = PoissonReconstruction(offsetBitmap, settings.DisplacementIterations);
                ApplyContrast(poissonHeightMap, settings.DisplacementContrast);

                if (settings.DisplacementSharpness > 0)
                {
                    poissonHeightMap = ApplyBilateralFilter(poissonHeightMap, settings.DisplacementSharpness);
                }

                NormalizeHeightMap(poissonHeightMap, out float poissonMin, out float poissonMax);

                // Save Poisson as 8-bit for comparison
                Save8BitDisplacementMap(poissonHeightMap, outDirectory, fileName + "_displacement_poisson");
            }

            // Export multi-scale version for comparison (if enabled)
            if (settings.UseMultiScaleProcessing)
            {
                var multiScaleHeightMap = ProcessMultiScale(offsetBitmap, settings);
                ApplyContrast(multiScaleHeightMap, settings.DisplacementContrast);

                if (settings.DisplacementSharpness > 0)
                {
                    multiScaleHeightMap = ApplyBilateralFilter(multiScaleHeightMap, settings.DisplacementSharpness);
                }

                NormalizeHeightMap(multiScaleHeightMap, out float multiMin, out float multiMax);

                // Save multi-scale as 8-bit for comparison
                Save8BitDisplacementMap(multiScaleHeightMap, outDirectory, fileName + "_displacement_multiscale");
            }
        }

        private float[,] StandardHeightMapGeneration(System.Drawing.Bitmap rawBitmap, int iterations)
        {
            int width = rawBitmap.Width;
            int height = rawBitmap.Height;
            float[,] heightMap = new float[width, height];

            // Convert normal map to initial grayscale using luminance (matching NormalMap-Online approach)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = rawBitmap.GetPixel(x, y);
                    float gray = (pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f) / 255.0f;
                    heightMap[x, y] = gray;
                }
            }

            // Apply iterative smoothing (relaxation/diffusion)
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
                Array.Copy(tempMap, heightMap, width * height);
            }

            return heightMap;
        }

        private float[,] PoissonReconstruction(System.Drawing.Bitmap rawBitmap, int iterations)
        {
            int width = rawBitmap.Width;
            int height = rawBitmap.Height;

            // Start with the same luminance-based initial height as standard method
            float[,] heightMap = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = rawBitmap.GetPixel(x, y);
                    float gray = (pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f) / 255.0f;
                    heightMap[x, y] = gray;
                }
            }

            // Extract gradients from normal map for refinement
            float[,] gradientX = new float[width, height];
            float[,] gradientY = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = rawBitmap.GetPixel(x, y);
                    // Convert from [0,255] to [-1,1] - normal map encoding
                    gradientX[x, y] = (pixel.R / 255.0f) * 2.0f - 1.0f;
                    gradientY[x, y] = (pixel.G / 255.0f) * 2.0f - 1.0f;
                }
            }

            // Solve Poisson equation using Jacobi iteration
            // Use fewer iterations and dampen the gradient influence to avoid noise amplification
            float[,] tempMap = new float[width, height];
            for (int iter = 0; iter < iterations; iter++) // Reduced from iterations * 5
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        // Divergence of gradient field
                        float div = (gradientX[x, y] - gradientX[x - 1, y]) +
                                   (gradientY[x, y] - gradientY[x, y - 1]);

                        // Laplacian: average of neighbors
                        float laplacian = (heightMap[x - 1, y] + heightMap[x + 1, y] +
                                          heightMap[x, y - 1] + heightMap[x, y + 1]) * 0.25f;

                        // Dampen the divergence influence to reduce noise (0.1 instead of 0.25)
                        tempMap[x, y] = laplacian - div * 0.1f;
                    }
                }
                Array.Copy(tempMap, heightMap, width * height);
            }

            // Normalize the Poisson result to 0-1 range before returning
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    minVal = Math.Min(minVal, heightMap[x, y]);
                    maxVal = Math.Max(maxVal, heightMap[x, y]);
                }
            }

            if (maxVal > minVal)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        heightMap[x, y] = (heightMap[x, y] - minVal) / (maxVal - minVal);
                    }
                }
            }

            return heightMap;
        }

        private float[,] ProcessMultiScale(System.Drawing.Bitmap rawBitmap, RmvToGltfExporterSettings settings)
        {
            int width = rawBitmap.Width;
            int height = rawBitmap.Height;

            // Process at full resolution - always use standard method for multi-scale
            var fullRes = StandardHeightMapGeneration(rawBitmap, settings.DisplacementIterations);

            // Process at half resolution - always use standard method for multi-scale
            using var halfBitmap = new System.Drawing.Bitmap(rawBitmap, width / 2, height / 2);
            var halfRes = StandardHeightMapGeneration(halfBitmap, settings.DisplacementIterations);

            // Upscale half resolution
            var halfUpscaled = UpscaleHeightMap(halfRes, width, height);

            // Blend full and upscaled half (70% full, 30% half for detail preservation)
            float[,] blended = new float[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    blended[x, y] = fullRes[x, y] * 0.7f + halfUpscaled[x, y] * 0.3f;
                }
            }

            return blended;
        }

        private float[,] UpscaleHeightMap(float[,] input, int targetWidth, int targetHeight)
        {
            int srcWidth = input.GetLength(0);
            int srcHeight = input.GetLength(1);
            float[,] output = new float[targetWidth, targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    float srcX = x * (srcWidth - 1f) / (targetWidth - 1f);
                    float srcY = y * (srcHeight - 1f) / (targetHeight - 1f);

                    int x0 = (int)srcX;
                    int y0 = (int)srcY;
                    int x1 = Math.Min(x0 + 1, srcWidth - 1);
                    int y1 = Math.Min(y0 + 1, srcHeight - 1);

                    float fx = srcX - x0;
                    float fy = srcY - y0;

                    // Bilinear interpolation
                    output[x, y] = input[x0, y0] * (1 - fx) * (1 - fy) +
                                   input[x1, y0] * fx * (1 - fy) +
                                   input[x0, y1] * (1 - fx) * fy +
                                   input[x1, y1] * fx * fy;
                }
            }

            return output;
        }

        private void ApplyContrast(float[,] heightMap, float contrastFactor)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = heightMap[x, y];
                    heightMap[x, y] = Math.Clamp((value - 0.5f) * (1.0f + contrastFactor) + 0.5f, 0, 1);
                }
            }
        }

        private float[,] ApplyBilateralFilter(float[,] heightMap, float strength)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            float[,] output = new float[width, height];

            const int radius = 2;
            float sigmaSpatial = 2.0f;
            float sigmaRange = 0.1f * strength;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0;
                    float totalWeight = 0;
                    float centerValue = heightMap[x, y];

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = Math.Clamp(x + dx, 0, width - 1);
                            int ny = Math.Clamp(y + dy, 0, height - 1);

                            float neighborValue = heightMap[nx, ny];

                            // Spatial weight (Gaussian based on distance)
                            float spatialDist = dx * dx + dy * dy;
                            float spatialWeight = (float)Math.Exp(-spatialDist / (2 * sigmaSpatial * sigmaSpatial));

                            // Range weight (Gaussian based on intensity difference)
                            float rangeDist = (centerValue - neighborValue) * (centerValue - neighborValue);
                            float rangeWeight = (float)Math.Exp(-rangeDist / (2 * sigmaRange * sigmaRange));

                            float weight = spatialWeight * rangeWeight;
                            sum += neighborValue * weight;
                            totalWeight += weight;
                        }
                    }

                    output[x, y] = totalWeight > 0 ? sum / totalWeight : centerValue;
                }
            }

            return output;
        }

        private void NormalizeHeightMap(float[,] heightMap, out float minHeight, out float maxHeight)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            minHeight = float.MaxValue;
            maxHeight = float.MinValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    minHeight = Math.Min(minHeight, heightMap[x, y]);
                    maxHeight = Math.Max(maxHeight, heightMap[x, y]);
                }
            }

            // Simple normalization: map the actual range to 0-1
            // This preserves the relative values without forcing expansion to extremes
            if (maxHeight > minHeight)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        heightMap[x, y] = (heightMap[x, y] - minHeight) / (maxHeight - minHeight);
                    }
                }
            }
            else
            {
                // All values are the same - set to middle grey
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        heightMap[x, y] = 0.5f;
                    }
                }
            }
        }

        private void Save16BitDisplacementMap(float[,] heightMap, string outDirectory, string fileName)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            // Create 16-bit grayscale data
            byte[] pixelData = new byte[width * height * 2]; // 2 bytes per pixel

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ushort value = (ushort)(heightMap[x, y] * 65535);
                    int index = (y * width + x) * 2;
                    pixelData[index] = (byte)(value >> 8);     // High byte
                    pixelData[index + 1] = (byte)(value & 0xFF); // Low byte
                }
            }

            // Save as 16-bit PNG using custom encoding
            var displacementPngPath = Path.Combine(outDirectory, fileName + "_displacement_16bit.png");

            // For now, save as 8-bit with note - true 16-bit PNG requires external library
            // System.Drawing doesn't support 16-bit grayscale directly
            Save8BitDisplacementMap(heightMap, outDirectory, fileName + "_displacement");

            // Also save raw 16-bit data for advanced users
            File.WriteAllBytes(Path.Combine(outDirectory, fileName + "_displacement_16bit.raw"), pixelData);
        }

        private void Save8BitDisplacementMap(float[,] heightMap, string outDirectory, string fileName)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            using var displacementBitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte grayscale = (byte)Math.Clamp(heightMap[x, y] * 255.0f, 0, 255);
                    displacementBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, grayscale, grayscale, grayscale));
                }
            }

            var displacementPngPath = Path.Combine(outDirectory, fileName + ".png");
            displacementBitmap.Save(displacementPngPath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
