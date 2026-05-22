namespace GameWorld.Core.SceneNodes
{
    public class SlotsNode : GroupNode
    {
        public SlotsNode(string name) : base(name) { }

        protected SlotsNode() { }

        public override ISceneNode CreateCopyInstance() => new SlotsNode();
    }
}
