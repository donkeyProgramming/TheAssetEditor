using Common;
using Filetypes.RigidModel;
using Filetypes.RigidModel.LodHeader;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using View3D.Animation;
using View3D.Services;

namespace View3D.SceneNodes
{
    public class MainEditableNode : Rmv2ModelNode
    {
        public SkeletonNode Skeleton { get; private set; }
        public IPackFile MainPackFile { get; private set; }
        public RmvVersionEnum SelectedOutputFormat { get; set; }

        public MainEditableNode(string name, SkeletonNode skeletonNode, IPackFile mainFile) : base(name)
        {
            Skeleton = skeletonNode;
            MainPackFile = mainFile;
        }

        public bool AreAllNodesVisible()
        {
            bool isAllVisible = true;
            GetLodNodes()[0].ForeachNodeRecursive((node) =>
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
            var lodCount = lods.Count;

            RmvSubModel[][] newMeshList = new RmvSubModel[orderedLods.Count()][];
            for (int currentLodIndex = 0; currentLodIndex < lodCount; currentLodIndex++)
            {
                List<Rmv2MeshNode> meshes = GetMeshesInLod(currentLodIndex, onlySaveVisibleNodes);

                newMeshList[currentLodIndex] = new RmvSubModel[meshes.Count];

                for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
                {
                    meshes[meshIndex].RecomputeBoundingBox();
                    newMeshList[currentLodIndex][meshIndex] = meshes[meshIndex].CreateRmvSubModel();
                    newMeshList[currentLodIndex][meshIndex].UpdateAttachmentPointList(boneNames);
                }
            }

            Model.MeshList = newMeshList;
            Model.UpdateOffsets();

            using MemoryStream ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);


            Model.SaveToByteArray(writer);
            var data = ms.ToArray();



            //var test = MeshSaverService.Save(onlySaveVisibleNodes, this, Skeleton.AnimationProvider.Skeleton);
            var test2 = MeshSaverService.Save(onlySaveVisibleNodes, new List< Rmv2ModelNode >(){ this}, Skeleton.AnimationProvider.Skeleton);


            for (int i = 0; i < test2.Length; i++)
            {
                if (data[i] != test2[i])
                { 
                }
            
            }

            return test2;

        }






        // GetAllNodes

        // GetAllMeshes

        // GetAllGroups


    }
}
