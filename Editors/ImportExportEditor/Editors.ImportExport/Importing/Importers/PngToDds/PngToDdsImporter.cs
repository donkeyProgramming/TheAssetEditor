using DirectXTexNet;
using Editors.ImportExport.Importing.Importers.PngToDds.Helpers;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.Types;
using Editors.ImportExport.Common.Interfaces;

namespace Editors.ImportExport.Importing.Importers.PngToDds
{

    public class PngToDdsImporter
    {
        static public PackFile Import(string inputPath, TextureType textureType, GameTypeEnum gameType, string outFileName)
        {
            ScratchImage scratchImagePng = TexHelper.Instance.LoadFromWICFile(inputPath, WIC_FLAGS.DEFAULT_SRGB);

            bool isUncompressed = scratchImagePng.GetMetadata().Format == DXGI_FORMAT.B8G8R8A8_UNORM || scratchImagePng.GetMetadata().Format == DXGI_FORMAT.B8G8R8A8_UNORM_SRGB;

            var processedImage = ImageProcessorFactory.CreateImageProcessor(textureType).Transform(scratchImagePng);
            // process image based on texture type

            var imageWithMips = processedImage.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, 0);
            var ddsFormat = DDSFormatHelper.GetDDSFormat(gameType, textureType);
            var ddsImage = imageWithMips.Compress(ddsFormat, TEX_COMPRESS_FLAGS.DEFAULT, 0.5f);

            var ddsMemStream = ddsImage.SaveToDDSMemory(DDS_FLAGS.NONE);

            byte[] ddsBytes = new byte[ddsMemStream.Length];
            ddsMemStream.Read(ddsBytes, 0, ddsBytes.Length);

            var ddsPackFile = new PackFile(outFileName, new MemorySource(ddsBytes));

            return ddsPackFile;
        }
    }
}
