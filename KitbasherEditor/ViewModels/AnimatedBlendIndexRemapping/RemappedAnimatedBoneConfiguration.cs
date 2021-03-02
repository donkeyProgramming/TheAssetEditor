using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using View3D.Rendering.Geometry;

namespace KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping
{
    public class RemappedAnimatedBoneConfiguration
    {

        public ObservableCollection<AnimatedBone> MeshBones { get; set; }
        public string MeshSkeletonName { get; set; }

        public ObservableCollection<AnimatedBone> ParentModelBones { get; set; }
       
        public string ParnetModelSkeletonName { get; set; }


        
    }
}
