using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.PackFiles;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Services
{
    public class WsModelMaterialProvider
    {
        private readonly IPackFileService _packFileService;
        private readonly WsModelFile? _wsModelFile;

        private WsModelMaterialProvider(IPackFileService packFileService, WsModelFile? wsModelFile)
        {
            _packFileService = packFileService;
            _wsModelFile = wsModelFile;
        }

        public static WsModelMaterialProvider CreateFromModelPath(IPackFileService packFileService, string rmv2ModelPath)
        {
            var wsModelPath = Path.ChangeExtension(rmv2ModelPath, ".wsmodel");
            return CreateFromWsModelPath(packFileService, wsModelPath);
        }

        public static WsModelMaterialProvider CreateFromWsModel(IPackFileService packFileService, WsModelFile wsModel)
        {
            return new WsModelMaterialProvider(packFileService, wsModel);
        }

        public static WsModelMaterialProvider CreateFromWsModelPath(IPackFileService packFileService, string wsModelPath)
        {
            var packFile = packFileService.FindFile(wsModelPath);
            if (packFile == null)
                return new WsModelMaterialProvider(packFileService, null);

            var wsModel = new WsModelFile(packFile);
            return CreateFromWsModel(packFileService, wsModel);
        }

        public WsModelMaterialFile? GetModelMaterial(int lodIndex, int partIndex)
        {
            if (_wsModelFile == null)
                return null;

            var materialPath = _wsModelFile.MaterialList.FirstOrDefault(x => x.LodIndex == lodIndex && x.PartIndex == partIndex);
            if (materialPath == null)
                return null;

            var wsMaterialPath = _packFileService.FindFile(materialPath.MaterialPath);
            if (wsMaterialPath == null)
                return null;

            return new WsModelMaterialFile(wsMaterialPath);
        }
    }
}


