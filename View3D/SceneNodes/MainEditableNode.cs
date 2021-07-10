using Common;
using Filetypes.RigidModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace View3D.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode Skeleton { get; private set; }
        public IPackFile MainPackFile { get; private set; }

        public MainEditableNode(string name, SkeletonNode skeletonNode, IPackFile mainFile) : base(name)
        {
            Skeleton = skeletonNode;
            MainPackFile = mainFile;
        }

        public bool AreAllNodesVisible()
        {
            bool isAllVisible = true;
            GetLodNodes()[0].ForeachNode((node) =>
            {
                if (!node.IsVisible)
                    isAllVisible = false;
            });
            return isAllVisible;
        }

        public byte[] Save(bool onlySaveVisibleNodes)
        {
            List<string> boneNames = new List<string>();
            if (Skeleton.AnimationProvider.Skeleton != null)
                boneNames = Skeleton.AnimationProvider.Skeleton.BoneNames.ToList();

            var lods = GetLodNodes();
            var orderedLods = lods.OrderBy(x => x.LodValue);

            RmvSubModel[][] newMeshList = new RmvSubModel[orderedLods.Count()][];
            for (int lodIndex = 0; lodIndex < orderedLods.Count(); lodIndex++)
            {
                var meshes = GetMeshesInLod(lodIndex, onlySaveVisibleNodes);

                newMeshList[lodIndex] = new RmvSubModel[meshes.Count];

                for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    meshes[meshIndex].RecomputeBoundingBox();
                    newMeshList[lodIndex][meshIndex] = meshes[meshIndex].CreateRmvSubModel();
                    newMeshList[lodIndex][meshIndex].UpdateAttachmentPointList(boneNames);
                }
            }

            Model.MeshList = newMeshList;
            Model.UpdateOffsets();

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);


            Model.SaveToByteArray(writer);
            return ms.ToArray();
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


        // GetAllNodes

        // GetAllMeshes

        // GetAllGroups


    }
}
