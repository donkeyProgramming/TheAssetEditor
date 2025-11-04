using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Editors.ImportExport.Importing.Importers.PngToDds.Helpers.ImageProcessor;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.ImportExport.Common.Interfaces
{
    public static class ImageProcessorFactory
    {
        private static readonly Dictionary<TextureType, IImageProcessor> _textureAndGameTypeToTranformer = new Dictionary<TextureType, IImageProcessor>()
        {
             {TextureType.Diffuse, new DefaultImageProcessor() },
             {TextureType.MaterialMap, new BlenderToWH3MaterialMapProcessor() },
             {TextureType.BaseColour, new DefaultImageProcessor() },
             {TextureType.Normal, new BlueToOrangeNormalMapProcessor() }
        };

        public static IImageProcessor CreateImageProcessor(TextureType textureType)
        {
            if (_textureAndGameTypeToTranformer.TryGetValue(textureType, out var imageProcessor))
            {                
                return imageProcessor;
            }

            return new DefaultImageProcessor();
        }
    }
}
