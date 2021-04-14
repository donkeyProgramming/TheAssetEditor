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
        SkeletonNode _skeletonNode;
        
        public MainEditableNode(string name) : base(name)
        {
            
        }


        public byte[] Save(bool onlySaveVisibleNodes, List<string> boneNames)
        {
            var lods = GetLodNodes();
            var orderedLods = lods.OrderBy(x => x.LodValue);

            RmvSubModel[][] newMeshList = new RmvSubModel[orderedLods.Count()][];
            for (int lodIndex = 0; lodIndex < orderedLods.Count(); lodIndex++)
            {
                var meshes = orderedLods.ElementAt(lodIndex).GetAllModels(onlySaveVisibleNodes);
                newMeshList[lodIndex] = new RmvSubModel[meshes.Count];

                for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
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

        // GetAllNodes

        // GetAllMeshes

        // GetAllGroups


    }
}
