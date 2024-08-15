using System.IO;
using System.Linq;
using Shared.Core.PackFiles;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Services
{
    public class WsModelMaterialProvider
    {
        private readonly PackFileService _packFileService;
        private readonly WsModelFile? _wsModelFile;

        private WsModelMaterialProvider(PackFileService packFileService, WsModelFile? wsModelFile)
        {
            _packFileService = packFileService;
            _wsModelFile = wsModelFile;
        }

        public static WsModelMaterialProvider CreateFromModelPath(PackFileService packFileService, string rmv2ModelPath)
        {
            var wsModelPath = Path.ChangeExtension(rmv2ModelPath, ".wsmodel");
            return CreateFromWsModel(packFileService, wsModelPath);
        }

        public static WsModelMaterialProvider CreateFromWsModel(PackFileService packFileService, string wsModelPath)
        {
            var packFile = packFileService.FindFile(wsModelPath);
            if (packFile == null)
                return new WsModelMaterialProvider(packFileService, null);

            var wsModel = new WsModelFile(packFile);
            return new WsModelMaterialProvider(packFileService, wsModel);
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


        //public WsModelMaterialFile? ResolveFromModelPath(string rmv2ModelPath, int lodIndex, int partIndex)
        //{
        //    
        //    return ResolveFromWsModel(wsModelPath, lodIndex, partIndex);    
        //}
        //
        //public WsModelMaterialFile? ResolveFromWsModel(string wsModelPath, int lodIndex, int partIndex)
        //{
        //
        //
        //}
    }
}


