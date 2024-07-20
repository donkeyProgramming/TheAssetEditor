using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.UiCommands
{
    public class DeleteMissingTexturesCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;

        public DeleteMissingTexturesCommand(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        internal void Execute(MainEditableNode mainNode)
        {
            var meshes = mainNode.GetMeshesInLod(0, false);
            foreach (var mesh in meshes)
            {
                var resolver = new MissingTextureResolver();
                resolver.DeleteMissingTextures(mesh, _packFileService);
            }
 
        }
    }
}
