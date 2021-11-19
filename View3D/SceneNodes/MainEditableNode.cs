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
            var test2 = MeshSaverService.Save(onlySaveVisibleNodes, new List< Rmv2ModelNode >(){ this}, Skeleton.AnimationProvider.Skeleton, SelectedOutputFormat, ModelMaterialEnum.default_type);
            return test2;

        }

    }
}
