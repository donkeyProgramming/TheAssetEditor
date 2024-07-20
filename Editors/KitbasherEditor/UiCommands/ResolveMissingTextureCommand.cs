using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.UiCommands
{
    public class ResolveMissingTextureCommand : IUiCommand
    {
        private readonly PackFileService _packFileService;

        public ResolveMissingTextureCommand(PackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public void Execute(Rmv2MeshNode meshNode)
        {
            var resolver = new MissingTextureResolver();
            resolver.DeleteMissingTextures(meshNode, _packFileService);
        }
    }
}
