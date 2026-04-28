namespace GameWorld.Core.SceneNodes
{
    public class SlotNode : GroupNode
    {
        public SlotNode(string name, string attachmentBoneName) : base(name) { AttachmentBoneName = attachmentBoneName; }
        public string AttachmentBoneName { get; set; }

        protected SlotNode() { }

        public override ISceneNode CreateCopyInstance() => new SlotNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as SlotNode;
            typedTarget.AttachmentBoneName = AttachmentBoneName;
            base.CopyInto(target);
        }
    }
}
