using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectXTexNet;
using Shared.Core.Settings;
using Shared.GameFormats.RigidModel.Types;

namespace Editors.ImportExport.Importing.Importers.PngToDds.Helpers
{
    public class DDSFormatHelper
    {
        static private readonly Dictionary<(GameTypeEnum, TextureType), DXGI_FORMAT> _gameTypeAndTextureTypeToFormat = new Dictionary<(GameTypeEnum, TextureType), DXGI_FORMAT>
        {
            // spec gloss material games
            {(GameTypeEnum.Warhammer, TextureType.Diffuse), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer, TextureType.Specular), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer, TextureType.Gloss), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Warhammer, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Warhammer, TextureType.Mask), DXGI_FORMAT.BC3_UNORM},

            {(GameTypeEnum.Warhammer2, TextureType.Diffuse), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer2, TextureType.Specular), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer2, TextureType.Gloss), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Warhammer2, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Warhammer2, TextureType.Mask), DXGI_FORMAT.BC3_UNORM},
            
            {(GameTypeEnum.Troy, TextureType.Diffuse), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Troy, TextureType.Specular), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Troy, TextureType.Gloss), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Troy, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Troy, TextureType.Mask), DXGI_FORMAT.BC3_UNORM},

            {(GameTypeEnum.Pharaoh, TextureType.Diffuse), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Pharaoh, TextureType.Specular), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Pharaoh, TextureType.Gloss), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Pharaoh, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Pharaoh, TextureType.Mask), DXGI_FORMAT.BC3_UNORM},

            // metal-roughness material games            
            {(GameTypeEnum.Warhammer3, TextureType.BaseColour), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer3, TextureType.MaterialMap), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer3, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Warhammer3, TextureType.Mask), DXGI_FORMAT.BC3_UNORM},

            // Keys for "Empty" WH3 RMV2, that contains "old" spec-gloss paths pointing nowhere
            {(GameTypeEnum.Warhammer3, TextureType.Diffuse), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer3, TextureType.Specular), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.Warhammer3, TextureType.Gloss), DXGI_FORMAT.BC3_UNORM},

            {(GameTypeEnum.ThreeKingdoms, TextureType.BaseColour), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.ThreeKingdoms, TextureType.MaterialMap), DXGI_FORMAT.BC1_UNORM_SRGB},
            {(GameTypeEnum.ThreeKingdoms, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.ThreeKingdoms, TextureType.Mask), DXGI_FORMAT.BC3_UNORM},            
            
            {(GameTypeEnum.RomeRemastered, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Rome2, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            {(GameTypeEnum.Attila, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},            
            {(GameTypeEnum.Arena, TextureType.Normal), DXGI_FORMAT.BC3_UNORM},
            
        };

        public static DXGI_FORMAT GetDDSFormat(GameTypeEnum gameType, TextureType textureType)
        {
            if (_gameTypeAndTextureTypeToFormat.TryGetValue((gameType, textureType), out var format))
                return format;

            // TODO: LOG No format found for gameType and textureType

            return DXGI_FORMAT.BC3_UNORM;                        
        }
    }
}
