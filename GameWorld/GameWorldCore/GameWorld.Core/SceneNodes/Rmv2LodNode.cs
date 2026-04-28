using System.Collections.Generic;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2LodNode : GroupNode
    {
        public int LodValue { get; set; }

        public Rmv2LodNode(string name, int lodIndex) : base(name)
        {
            LodValue = lodIndex;
        }

        public List<Rmv2MeshNode> GetAllModels(bool onlyVisible)
        {
            var output = new List<Rmv2MeshNode>();
            foreach (var child in Children)
            {
                if (child is Rmv2MeshNode meshNode)
                {
                    if (!(onlyVisible && meshNode.IsVisible == false))
                        output.Add(meshNode);
                }
                else if (child is GroupNode groupNode)
                {
                    if (!(onlyVisible && groupNode.IsVisible == false))
                    {
                        foreach (var groupChild in child.Children)
                        {
                            if (groupChild is Rmv2MeshNode meshNode2)
                            {
                                if (!(onlyVisible && meshNode2.IsVisible == false))
                                    output.Add(meshNode2);
                            }
                        }
                    }
                }
            }

            return output;
        }

        public Dictionary<GroupNode, List<Rmv2MeshNode>> GetAllModelsGrouped(bool onlyVisible)
        {
            var output = new Dictionary<GroupNode, List<Rmv2MeshNode>>();

            foreach (var child in Children)
            {
                if (child is Rmv2MeshNode meshNode)
                {
                    if (!(onlyVisible && meshNode.IsVisible == false))
                    {
                        var parentAsGroup = meshNode.Parent as GroupNode;
                        if (output.ContainsKey(parentAsGroup) == false)
                            output.Add(parentAsGroup, new List<Rmv2MeshNode>());
                        output[parentAsGroup].Add(meshNode);
                    }
                }
                else if (child is GroupNode groupNode)
                {
                    if (!(onlyVisible && groupNode.IsVisible == false))
                    {
                        foreach (var groupChild in child.Children)
                        {
                            if (groupChild is Rmv2MeshNode meshNodeInGroup)
                            {
                                if (!(onlyVisible && meshNodeInGroup.IsVisible == false))
                                {
                                    if (output.ContainsKey(groupNode) == false)
                                        output.Add(groupNode, new List<Rmv2MeshNode>());
                                    output[groupNode].Add(meshNodeInGroup);

                                }
                            }
                        }
                    }
                }
            }

            return output;
        }

        protected Rmv2LodNode() { }

        public override ISceneNode CreateCopyInstance() => new Rmv2LodNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as Rmv2LodNode;
            typedTarget.LodValue = LodValue;
            base.CopyInto(target);
        }
    }
}
