using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public class MeshEditorViewModel : NotifyPropertyChangedImpl, ISceneNodeEditor
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _animLookUp;
        private readonly SceneManager _sceneManager;
        private readonly ApplicationSettingsService _applicationSettings;
        private readonly IUiCommandFactory _uiCommandFactory;

        public MeshViewModel Mesh { get; set; }
        public AnimationViewModel Animation { get; set; }
        public MaterialGeneralViewModel MaterialGeneral { get; set; }
        public WeightedMaterialViewModel Material { get; set; }

        public MeshEditorViewModel(KitbasherRootScene kitbasherRootScene,
            PackFileService pfs,
            SkeletonAnimationLookUpHelper animLookUp,
            SceneManager sceneManager,
            ApplicationSettingsService applicationSettings,
            IUiCommandFactory uiCommandFactory)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _pfs = pfs;
            _animLookUp = animLookUp;
            _sceneManager = sceneManager;
            _applicationSettings = applicationSettings;
            _uiCommandFactory = uiCommandFactory;
        }

        public void Initialize(ISceneNode node)
        {
            var typedNode = node as Rmv2MeshNode;

            Mesh = new MeshViewModel(typedNode, _sceneManager);
            Animation = new AnimationViewModel(_kitbasherRootScene, typedNode, _pfs, _animLookUp);
            MaterialGeneral = new MaterialGeneralViewModel(_kitbasherRootScene, typedNode, _pfs, _applicationSettings, _uiCommandFactory);

            if (typedNode.Material is WeightedMaterial)
                Material = new WeightedMaterialViewModel(typedNode);
        }

        public void Dispose()
        {

        }
    }
}
