using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.MathViews;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using View3D.Animation;
using View3D.Components.Rendering;

namespace AnimationEditor.AnimationBuilder
{

    public class AnimationNode
    {
        public NotifyAttr<uint> ComputeOrder { get; set; }
        public NotifyAttr<string> Name { get; set; }

        public ObservableCollection<AnimationEditorItem> EditItems { get; set; } = new ObservableCollection<AnimationEditorItem>();

        AnimationClip _inputAnimation;
        AnimationClip _outputAnimation;
    }

    public class AnimationEditorItem : NotifyPropertyChangedImpl
    {
        public NotifyAttr<bool> IsActive { get; set; }
        public NotifyAttr<string> Name { get; set; }
        public NotifyAttr<string> Description { get; set; }

        public NotifyAttr<bool> HasError { get; set; }
        public NotifyAttr<string> ErrorText { get; set; }
    }


    public class Editor : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _techSkeletonNode;
        IComponentManager _componentManager;
        CopyPasteManager _copyPasteManager;

        public ObservableCollection<AnimationNode> AnimationNodes { get; set; } = new ObservableCollection<AnimationNode>();

        public Editor(PackFileService pfs, AssetViewModel techSkeletonNode, IComponentManager componentManager, CopyPasteManager copyPasteManager)
        {
            _pfs = pfs;
            _techSkeletonNode = techSkeletonNode;
            _componentManager = componentManager;
            _copyPasteManager = copyPasteManager;
        }

        public void CreateEditor(AnimationBuilderInput input)
        {
            var node0 = new AnimationNode()
            {
                ComputeOrder = new NotifyAttr<uint>(0),
                Name = new NotifyAttr<string>("Arm animation"),
                EditItems = new ObservableCollection<AnimationEditorItem>()
                {
                    new AnimationEditorItem()
                    {
                        Name = new NotifyAttr<string>("Splice"),
                        Description = new NotifyAttr<string>("Doing some splicnig"),
                        IsActive = new NotifyAttr<bool>(false),
                    },
                     new AnimationEditorItem()
                    {
                        Name = new NotifyAttr<string>("Splice"),
                        Description = new NotifyAttr<string>("Doing some splicnig 2"),
                        IsActive = new NotifyAttr<bool>(false),
                    },
                    new AnimationEditorItem()
                    {
                        Name = new NotifyAttr<string>("Output frame count:"),
                        Description = new NotifyAttr<string>("Changing frame count"),
                        IsActive = new NotifyAttr<bool>(true),
                    }
                }
            };

            var node1 = new AnimationNode()
            {
                ComputeOrder = new NotifyAttr<uint>(0),
                Name = new NotifyAttr<string>("Output Animation"),
                EditItems = new ObservableCollection<AnimationEditorItem>()
                {
                    new AnimationEditorItem()
                    {
                        Name = new NotifyAttr<string>("Splice"),
                        Description = new NotifyAttr<string>("Doing some splicnig"),
                        IsActive = new NotifyAttr<bool>(false),
                    },
                     new AnimationEditorItem()
                    {
                        Name = new NotifyAttr<string>("Add"),
                        Description = new NotifyAttr<string>("Doing some adding"),
                        IsActive = new NotifyAttr<bool>(false),
                    },
                }
            };


            AnimationNodes.Add(node0);
            AnimationNodes.Add(node1);

            //try
            //{
            //    //UpdateSelectedBoneValues(null);
            //    //var packFile = _pfs.FindFile(skeletonPath);
            //    //SkeletonName.Value = skeletonPath;
            //    //_techSkeletonNode.SetSkeleton(packFile);
            //    //RefreshBoneList();
            //    //IsTechSkeleton = skeletonPath.ToLower().Contains("tech");
            //    //SourceSkeletonName.Value = _techSkeletonNode.Skeleton.SkeletonName;
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show($"Unable to load skeleton '{skeletonPath}'\n\n" + e.Message);
            //}
        }
    }
}
