using CommonControls.FileTypes.RigidModel.LodHeader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace View3D.SceneNodes
{
    public class Rmv2LodNode : GroupNode
    {
        public float? CameraDistance { get; set; }
        public float LodReductionFactor { get; set; } = -1;
        public bool AllowCombiningOfModelsForLodGeneration { get; set; } = false;
        public int LodValue { get; set; }
        public bool OptimizeLod { get; set; } = false;

        public Rmv2LodNode(string name, int lodIndex, float? cameraDistance=null) : base(name)
        {
            LodValue = lodIndex;
            CameraDistance = cameraDistance;
            AllowCombiningOfModelsForLodGeneration = false;

            if (lodIndex >= 2)
                OptimizeLod = true;
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
                    if ( !(onlyVisible && groupNode.IsVisible == false) )
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
