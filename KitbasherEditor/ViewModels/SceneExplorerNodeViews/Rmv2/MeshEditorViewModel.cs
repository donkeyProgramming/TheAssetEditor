using CommonControls.Common;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.Services;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews.Rmv2
{
    public class MeshEditorViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        public MeshViewModel Mesh { get; set; }
        public AnimationViewModel Animation { get; set; }
        public MaterialGeneralViewModel MaterialGeneral { get; set; }
        public WeightedMaterialViewModel Material { get; set; }

        public MeshEditorViewModel(KitbasherRootScene kitbasherRootScene, Rmv2MeshNode node, PackFileService pfs, SkeletonAnimationLookUpHelper animLookUp, SceneManager sceneManager, ApplicationSettingsService applicationSettings)
        {
            Mesh = new MeshViewModel(node, sceneManager);
            Animation = new AnimationViewModel(kitbasherRootScene, node, pfs, animLookUp);
            MaterialGeneral = new MaterialGeneralViewModel(kitbasherRootScene, node, pfs, applicationSettings);

            if (node.Material is WeightedMaterial)
                Material = new WeightedMaterialViewModel(node);
        }

        public void Dispose()
        {

        }
    }
}
