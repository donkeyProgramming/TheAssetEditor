using GameWorld.Core.SceneNodes;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.UiCommands
{
    public class CopyTexturesToPackCommand : IUiCommand
    {
        private readonly IPackFileService _packFileService;

        public CopyTexturesToPackCommand(IPackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        internal void Execute(MainEditableNode mainNode)
        {
            var meshes = mainNode.GetMeshesInLod(0, false);
            var materials = meshes.Select(x => x.RmvMaterial);
            var allTextures = materials.SelectMany(x => x.GetAllTextures());
            var distinctTextures = allTextures.DistinctBy(x => x.Path);

            foreach (var tex in distinctTextures)
            {
                var file = _packFileService.FindFile(tex.Path);
                if (file != null)
                {
                    var sourcePackContainer = _packFileService.GetPackFileContainer(file);
                    _packFileService.CopyFileFromOtherPackFile(sourcePackContainer, tex.Path, _packFileService.GetEditablePack());
                }
            }
        }
    }
}
