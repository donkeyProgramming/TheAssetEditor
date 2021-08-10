namespace View3D.SceneNodes
{
    public class SlotNode : GroupNode
    {
        public SlotNode(string name, string attachmentBoneName) : base(name) { AttachmentBoneName = attachmentBoneName; }
        public string AttachmentBoneName { get; set; }
    }

 
}
