using Filetypes.RigidModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.SceneNodes;

namespace View3D.Services
{
    public class Rmv2MeshNodeHelper
    {
        public static List<Rmv2MeshNode> GetAllVisibleMeshes(ISceneNode node)
        {
            return GetAllChildren(node);
        }

        static List<Rmv2MeshNode> GetAllChildren(ISceneNode parent)
        {
            var output = new List<Rmv2MeshNode>();
            var visibleChildren = parent.Children.Where(x => x.IsVisible);

            foreach (var child in visibleChildren)
            {
                if (child is Rmv2MeshNode mesh && mesh.LodIndex == 0)
                {
                    output.Add(mesh);
                }
                else
                {
                    var result = GetAllChildren(child);
                    output.AddRange(result);
                }
            }

            return output;
        }

        static List<ISceneNode> GetAllVisibleChildren(ISceneNode parent)
        {
            var output = new List<ISceneNode>();
            var visibleChildren = parent.Children.Where(x => x.IsVisible);

            foreach (var child in visibleChildren)
            {
                var result = GetAllChildren(child);
                output.AddRange(result);
            }

            return output;
        }


       //public void stuff(ISceneNode parent, GameSkeleton skeleton)
       //{ 
       //
       //}
       //
       //public byte[] Save(bool onlySaveVisibleNodes, ISceneNode rootNode, GameSkeleton skeleton)
       //{
       //    List<string> boneNames = new List<string>();
       //    if (skeleton != null)
       //        boneNames = skeleton.BoneNames.ToList();
       //
       //
       //    var modelNodes = GetAllVisibleChildren(rootNode)
       //        .Where(x => x is Rmv2ModelNode)
       //        .Select(x => x as Rmv2ModelNode).ToList();
       //
       //    var versions = modelNodes.Select(X => X.Model.Header.Version).Distinct();
       //    if (versions.Count() != 1)
       //        throw new Exception("Scene contains multiple header versions.");
       //
       //    var lodNodes = 
       //
       //
       //    var lods = GetLodNodes(rootNode);
       //    var orderedLods = lods.OrderBy(x => x.LodValue);
       //
       //    RmvSubModel[][] newMeshList = new RmvSubModel[orderedLods.Count()][];
       //    for (int lodIndex = 0; lodIndex < orderedLods.Count(); lodIndex++)
       //    {
       //        var meshes = GetMeshesInLod(rootNode, lodIndex, onlySaveVisibleNodes);
       //
       //        newMeshList[lodIndex] = new RmvSubModel[meshes.Count];
       //
       //        for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
       //        {
       //            meshes[meshIndex].RecomputeBoundingBox();
       //            newMeshList[lodIndex][meshIndex] = meshes[meshIndex].CreateRmvSubModel();
       //            newMeshList[lodIndex][meshIndex].UpdateAttachmentPointList(boneNames);
       //        }
       //    }
       //
       //    Model.MeshList = newMeshList;
       //    Model.UpdateOffsets();
       //
       //    using MemoryStream ms = new MemoryStream();
       //    using var writer = new BinaryWriter(ms);
       //
       //
       //    Model.SaveToByteArray(writer);
       //    return ms.ToArray();
       //}
       //
       //public List<Rmv2MeshNode> GetMeshesInLod(Rmv2LodNode  int lodIndex, bool onlyVisible)
       //{
       //    var lods = GetLodNodes();
       //    var orderedLods = lods.OrderBy(x => x.LodValue);
       //
       //    var meshes = orderedLods
       //       .ElementAt(lodIndex)
       //       .GetAllModels(onlyVisible);
       //
       //    return meshes;
       //}
       //
       //public List<Rmv2LodNode> GetLodNodes(ISceneNode rootNode)
       //{
       //    return rootNode.Children
       //        .Where(x => x is Rmv2LodNode)
       //        .Select(x => x as Rmv2LodNode)
       //        .ToList();
       //}
       //
       //
       //

    }
}
