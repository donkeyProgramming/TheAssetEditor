using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.Emissive;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
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
        private readonly CapabilityMaterialFactory _materialFactory;

        Rmv2MeshNode? _currentNode;

        [ObservableProperty] List<CapabilityMaterialsEnum> _possibleMaterialTypes;
        [ObservableProperty] CapabilityMaterialsEnum? _currentMaterialType;

        [ObservableProperty] DefaultViewModel? _default;
        [ObservableProperty] BloodViewModel? _blood;
        [ObservableProperty] EmissiveViewModel? _emissive;
        [ObservableProperty] TintViewModel? _tint;

        public WsMaterialViewModel(IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary, CapabilityMaterialFactory abstractMaterialFactory)
        {
            _uiCommandFactory = uiCommandFactory;
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
            _materialFactory = abstractMaterialFactory;

            _possibleMaterialTypes = _materialFactory.GetPossibleMaterials();
        }

        internal void Initialize(Rmv2MeshNode node)
        {
            _currentNode = node;
            var material = _currentNode.Effect;
            CurrentMaterialType = material.Type;

            var defaultCapability = material.TryGetCapability<MetalRoughCapability>();
            if(defaultCapability != null)
                Default = new DefaultViewModel(defaultCapability, _uiCommandFactory, _packFileService, _resourceLibrary);

            var bloodCapability = material.TryGetCapability<BloodCapability>();
            if (bloodCapability != null)
                Blood = new BloodViewModel(bloodCapability, _uiCommandFactory, _packFileService, _resourceLibrary);

            var emissiveCapability = material.TryGetCapability<EmissiveCapability>();
            if (emissiveCapability != null)
                Emissive = new EmissiveViewModel(emissiveCapability, _uiCommandFactory, _packFileService, _resourceLibrary);

            var tintCapability = material.TryGetCapability<TintCapability>();
            if (tintCapability != null)
                Tint = new TintViewModel(tintCapability);
        }

        partial void OnCurrentMaterialTypeChanged(CapabilityMaterialsEnum? oldValue, CapabilityMaterialsEnum? newValue)
        {
            Guard.IsNotNull(_currentNode);
            if (oldValue == newValue || newValue == null || oldValue == null)
                return;

            var newMaterial = _materialFactory.ChangeMaterial(_currentNode.Effect, newValue.Value);
            _currentNode.Effect = newMaterial;
        }
    }
}
