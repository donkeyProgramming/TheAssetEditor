using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;
using Editors.ImportExport.Common.Interfaces;
using Editors.ImportExport.Importing.Importers.PngToDds.Helpers;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.ImportExport.Importing.Importers.PngToDds
{
    public class PngToDdsImporter
    {
        public static PackFile Import(string inputPath, TextureType textureType, GameTypeEnum gameType, string outFileName, bool convertSpecialTexture = true, float alphaThreshold = 0.5f)
        {
            using var scratchImagePng = TexHelper.Instance.LoadFromWICFile(inputPath, WIC_FLAGS.DEFAULT_SRGB);
            return Import(scratchImagePng, textureType, gameType, outFileName, convertSpecialTexture, alphaThreshold);
        }

        /// <summary>
        /// For images that don't exist as files on disk, such as textures embedded in a .glb.
        /// </summary>
        public static PackFile Import(Stream inputStream, TextureType textureType, GameTypeEnum gameType, string outFileName, bool convertSpecialTexture = true, float alphaThreshold = 0.5f)
        {
            using var memoryStream = new MemoryStream();
            inputStream.CopyTo(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var imageHandle = GCHandle.Alloc(imageBytes, GCHandleType.Pinned);
            try
            {
                using var scratchImagePng = TexHelper.Instance.LoadFromWICMemory(imageHandle.AddrOfPinnedObject(), imageBytes.LongLength, WIC_FLAGS.DEFAULT_SRGB);
                return Import(scratchImagePng, textureType, gameType, outFileName, convertSpecialTexture, alphaThreshold);
            }
            finally
            {
                imageHandle.Free();
            }
        }

        private static PackFile Import(ScratchImage scratchImagePng, TextureType textureType, GameTypeEnum gameType, string outFileName, bool convertSpecialTexture, float alphaThreshold)
        {
            using var processedImage = ImageProcessorFactory.CreateImageProcessor(textureType, convertSpecialTexture).Transform(scratchImagePng);
            using var imageWithMips = processedImage.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, 0);
            var ddsFormat = DDSFormatHelper.GetDDSFormat(gameType, textureType);
            using var ddsImage = imageWithMips.Compress(ddsFormat, TEX_COMPRESS_FLAGS.DEFAULT, alphaThreshold);

            using var ddsMemStream = ddsImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            var ddsBytes = new byte[ddsMemStream.Length];
            ddsMemStream.Read(ddsBytes, 0, ddsBytes.Length);

            return new PackFile(outFileName, new MemorySource(ddsBytes));
        }
    }
}
