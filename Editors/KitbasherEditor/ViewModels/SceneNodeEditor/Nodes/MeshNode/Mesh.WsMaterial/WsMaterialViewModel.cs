using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.SceneNodes;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews
{
    public partial class WsMaterialViewModel : ObservableObject
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        [ObservableProperty] DefaultViewModel? _default;
        [ObservableProperty] BloodViewModel? _blood;

        public WsMaterialViewModel(IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _uiCommandFactory = uiCommandFactory;
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        internal void Initialize(Rmv2MeshNode typedNode)
        {
            var defaultCapability = typedNode.Effect.TryGetCapability<DefaultCapability>();
            if(defaultCapability != null)
                Default = new DefaultViewModel(defaultCapability, _uiCommandFactory, _packFileService, _resourceLibrary);

            var bloodCapability = typedNode.Effect.TryGetCapability<BloodCapability>();
            if (bloodCapability != null)
                Blood = new BloodViewModel(bloodCapability, _uiCommandFactory, _packFileService, _resourceLibrary);

        }
    }
}
