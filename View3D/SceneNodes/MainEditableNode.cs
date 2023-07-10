using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using System.IO;
using View3D.Animation;
using View3D.Services;

namespace View3D.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        private readonly PackFileService _pfs;

        public SkeletonNode SkeletonNode { get; private set; }
        public RmvVersionEnum SelectedOutputFormat { get; set; }
        public TextureFileEditorService TextureFileEditorService { get; set; }
        AnimationPlayer _player;

        public MainEditableNode(AnimationPlayer player, string name, SkeletonNode skeletonNode, PackFileService pfs) : base(name)
        {
            _pfs = pfs;
            _player = player;
            SkeletonNode = skeletonNode;
            TextureFileEditorService = new TextureFileEditorService(this, pfs);
        }

        public void SetSkeletonFromName(string skeletonName)
        {
            string cleanSkeletonName = "";
            if (!string.IsNullOrWhiteSpace(skeletonName))
                cleanSkeletonName = Path.GetFileNameWithoutExtension(skeletonName);

            string animationFolder = "animations\\skeletons\\";
            var skeletonFilePath = animationFolder + cleanSkeletonName + ".anim";
            var skeletonPfs = _pfs.FindFile(skeletonFilePath);
            if (skeletonPfs != null)
            {
                var animClip = AnimationFile.Create(skeletonPfs);
                SkeletonNode.Skeleton = new GameSkeleton(animClip, _player);
            }
        }
    }
}
