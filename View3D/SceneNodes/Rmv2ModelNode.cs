using Filetypes.RigidModel;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Rendering.Geometry;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2ModelNode : GroupNode
    {
        public RmvRigidModel Model { get; set; }

        public Rmv2ModelNode(RmvRigidModel model,  ResourceLibary resourceLib, string name, AnimationPlayer animationPlayer, IGeometryGraphicsContextFactory contextFactory) : base(name)
        {
            Name = name;

            for (int lodIndex = 0; lodIndex < model.LodHeaders.Count(); lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex)
                {
                    IsVisible = lodIndex == 0
                };
                AddObject(lodNode);
            }

            SetModel(model, resourceLib, animationPlayer, contextFactory);
        }

        public Rmv2ModelNode(string name)
        {
            Name = name;

            for (int lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex)
                {
                    IsVisible = lodIndex == 0
                };
                AddObject(lodNode);
            }
        }

        public void SetModel(RmvRigidModel model, ResourceLibary resourceLibary, AnimationPlayer animationPlayer, IGeometryGraphicsContextFactory contextFactory)
        {
            Model = model;
            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                if (lodIndex >= Children.Count)
                    AddObject(new Rmv2LodNode("Lod " + lodIndex, lodIndex));

                var lodNode = Children[lodIndex];
                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var node = new Rmv2MeshNode(model.MeshList[lodIndex][modelIndex], model.Header.SkeletonName, contextFactory.Create(), resourceLibary, animationPlayer);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }
            }
        }

        public List<Rmv2LodNode> GetLodNodes()
        {
            return Children
                .Where(x => x is Rmv2LodNode)
                .Select(x => x as Rmv2LodNode)
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
            var lods = GetLodNodes();
            return lods[lod].Children.Select(x=> x as Rmv2MeshNode).ToList();
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

        public override void CopyInto(ISceneNode tartet)
        {
            var typedTarget = tartet as Rmv2ModelNode;
            typedTarget.Model = Model;
            base.CopyInto(tartet);
        }
    }

 
}


