using CommunityToolkit.Mvvm.ComponentModel;
using Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.Rmv2;
using Editors.Shared.Core.Services;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using KitbasherEditor.ViewModels;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2;
using KitbasherEditor.Views.EditorViews.Rmv2;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.Ui.Common.DataTemplates;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class MeshEditorViewModel : ObservableObject, ISceneNodeEditor, IViewProvider<MeshEditorView>
    {
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly PackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _animLookUp;
        private readonly SceneManager _sceneManager;
        private readonly ApplicationSettingsService _applicationSettings;
        private readonly IUiCommandFactory _uiCommandFactory;

        [ObservableProperty] MeshViewModel _mesh;
        [ObservableProperty] AnimationViewModel _animation;
        [ObservableProperty] MaterialGeneralViewModel _materialGeneral;
        [ObservableProperty] WeightedMaterialViewModel _material;

        public MeshEditorViewModel(KitbasherRootScene kitbasherRootScene,
            PackFileService pfs,
            SkeletonAnimationLookUpHelper animLookUp,
            SceneManager sceneManager,
            ApplicationSettingsService applicationSettings,
            IUiCommandFactory uiCommandFactory,
            MeshViewModel meshViewModel)
        {
            _kitbasherRootScene = kitbasherRootScene;
            _pfs = pfs;
            _animLookUp = animLookUp;
            _sceneManager = sceneManager;
            _applicationSettings = applicationSettings;
            _uiCommandFactory = uiCommandFactory;

            _mesh = meshViewModel;
        }

        public void Initialize(ISceneNode node)
        {
            var typedNode = node as Rmv2MeshNode;

            Mesh.Initialize(typedNode);

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
