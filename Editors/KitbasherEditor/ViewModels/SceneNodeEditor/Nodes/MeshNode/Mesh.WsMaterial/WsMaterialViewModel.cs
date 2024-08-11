using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.DirtAndDecal;
using Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.Emissive;
using Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.SpecGloss;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using SharpDX.Direct3D11;

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

        [ObservableProperty] MetalRoughViewModel? _metalRough;
        [ObservableProperty] SpecGlossViewModel? _specGloss;
        [ObservableProperty] DirtAndDecalViewModel? _decalAndDirt;
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

            MetalRough = CreateViewModel<MetalRoughCapability, MetalRoughViewModel>(material, (cap) => new MetalRoughViewModel(cap, _uiCommandFactory, _packFileService, _resourceLibrary));
            SpecGloss = CreateViewModel<SpecGlossCapability, SpecGlossViewModel>(material, (cap) => new SpecGlossViewModel(cap, _uiCommandFactory, _packFileService, _resourceLibrary));
            DecalAndDirt = CreateViewModel<DecalAndDirtCapability, DirtAndDecalViewModel>(material, (cap) => new DirtAndDecalViewModel(cap, _uiCommandFactory, _packFileService, _resourceLibrary));
            Blood = CreateViewModel<BloodCapability, BloodViewModel>(material, (cap) => new BloodViewModel(cap, _uiCommandFactory, _packFileService, _resourceLibrary));
            Emissive = CreateViewModel<EmissiveCapability, EmissiveViewModel>(material, (cap) => new EmissiveViewModel(cap, _uiCommandFactory, _packFileService, _resourceLibrary));
            Tint = CreateViewModel<TintCapability, TintViewModel>(material, (cap) => new TintViewModel(cap));
        }

        partial void OnCurrentMaterialTypeChanged(CapabilityMaterialsEnum? oldValue, CapabilityMaterialsEnum? newValue)
        {
            Guard.IsNotNull(_currentNode);
            if (oldValue == newValue || newValue == null || oldValue == null)
                return;

            var newMaterial = _materialFactory.ChangeMaterial(_currentNode.Effect, newValue.Value);
            _currentNode.Effect = newMaterial;
        }

        TViewModel? CreateViewModel<T, TViewModel>(CapabilityMaterial material, Func<T, TViewModel> creator)
            where T : class, ICapability
            where TViewModel : class
        {
            var capability = material.TryGetCapability<T>();
            if (capability != null)
                return creator(capability);
            return null;
        }
    }
}
