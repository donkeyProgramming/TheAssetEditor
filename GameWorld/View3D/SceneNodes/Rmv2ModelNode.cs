﻿using System.Collections.Generic;
using System.Linq;

namespace GameWorld.Core.SceneNodes
{
    public class Rmv2ModelNode : GroupNode
    {
        public Rmv2ModelNode(string name)
        {
            Name = name;
        }

        public List<Rmv2LodNode> GetLodNodes()
        {
            return Children
                .Where(x => x is Rmv2LodNode)
                .Select(x => x as Rmv2LodNode)
                .Cast<Rmv2LodNode>()
                .ToList();
        }

        public Rmv2MeshNode GetMeshNode(int lod, int modelIndex)
        {
            var lods = GetLodNodes();
            while (lods.Count <= lod)
            {
                Children.Add(new Rmv2LodNode("Test", 12));
                lods = GetLodNodes();
            }

            if (lods[lod].Children.Count <= modelIndex)
                return null;

            return lods[lod].Children[modelIndex] as Rmv2MeshNode;
        }

        public List<Rmv2MeshNode> GetMeshNodes(int lod)
        {
            var lodNodes = GetLodNodes();
            var lodNode = lodNodes[lod];
            var meshes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(lodNode);
            return meshes;
        }

        public List<Rmv2MeshNode> GetMeshesInLod(int lodIndex, bool onlyVisible)
        {
            var lods = GetLodNodes();
            var orderedLods = lods.OrderBy(x => x.LodValue);

            var meshes = orderedLods
               .ElementAt(lodIndex)
               .GetAllModels(onlyVisible);

            return meshes;
        }


        protected Rmv2ModelNode() { }

        public override ISceneNode CreateCopyInstance() => new Rmv2ModelNode();

    }
}


