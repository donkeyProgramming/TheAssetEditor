using Common;
using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.Editors.BoneMapping
{
    public class AnimatedBone : NotifyPropertyChangedImpl
    {
        string _name;
        public string Name { get { return _name; } set { _name = value; } }

        int _boneIndex;
        public int BoneIndex { get { return _boneIndex; } set { _boneIndex = value; } }

        public ObservableCollection<AnimatedBone> Children { get; set; } = new ObservableCollection<AnimatedBone>();

        public bool IsUsedByCurrentModel { get; set; } = false;

        string _mappedBoneName;
        public string MappedBoneName { get { return _mappedBoneName; } set { SetAndNotify(ref _mappedBoneName, value); } }

        int _mappedBoneIndex = -1;
        public int MappedBoneIndex { get { return _mappedBoneIndex; } set { SetAndNotify(ref _mappedBoneIndex, value); } }


        //public NotifyAttr<bool> CanPreview { get; set; } = new NotifyAttr<bool>(false);asdasd



        // Meta data
        public Vector3 BonePosOffset { get; set; } = new Vector3(0);
        public Vector3 BoneRotOffset { get; set; } = new Vector3(0);
        public float BoneScaleOffset { get; set; } = 1;

        bool isVisible = true;
        [JsonIgnore]
        public bool IsVisible { get { return isVisible; } set { SetAndNotify(ref isVisible, value); } }

        public void ClearMapping()
        {
            MappedBoneName = "";
            MappedBoneIndex = -1;

            foreach (var child in Children)
                child.ClearMapping();
        }

        public AnimatedBone GetFromBoneId(int i)
        {
            if (BoneIndex == i)
                return this;

            foreach (var child in Children)
            {
                var res = child.GetFromBoneId(i);
                if (res != null)
                    return res;
            }

            return null;
        }

        public override string ToString()
        {
            return Name + " -> " + MappedBoneName;
        }
    }


    public class AnimatedBoneHelper
    {
        public static List<IndexRemapping> BuildRemappingList(AnimatedBone bone)
        {
            var output = new List<IndexRemapping>();
            RecursiveBuildMappingList(bone, output);
            return output;
        }

        static void RecursiveBuildMappingList(AnimatedBone bone, List<IndexRemapping> output)
        {
            if (bone.MappedBoneIndex != -1)
            {
                var mapping = new IndexRemapping((byte)bone.BoneIndex, (byte)bone.MappedBoneIndex, bone.IsUsedByCurrentModel);
                output.Add(mapping);
            }

            foreach (var child in bone.Children)
                RecursiveBuildMappingList(child, output);
        }


        public static ObservableCollection<AnimatedBone> CreateFromSkeleton(AnimationFile file, List<int> boneIndexUsedByModel = null)
        {
            var output = new ObservableCollection<AnimatedBone>();

            foreach (var boneInfo in file.Bones)
            {
                var parent = FindBoneInList(boneInfo.ParentId, output);
                var newNode = new AnimatedBone() { BoneIndex = boneInfo.Id, Name = boneInfo.Name };
                if (boneIndexUsedByModel == null)
                    newNode.IsUsedByCurrentModel = true;
                else
                    newNode.IsUsedByCurrentModel = boneIndexUsedByModel.Contains((byte)boneInfo.Id);

                if (parent == null)
                    output.Add(newNode);
                else
                    parent.Children.Add(newNode);
            }
            return output;
        }

        static AnimatedBone FindBoneInList(int parentId, IEnumerable<AnimatedBone> boneList)
        {
            foreach (var bone in boneList)
            {
                if (bone.BoneIndex == parentId)
                    return bone;
            }

            foreach (var bone in boneList)
            {
                var res = FindBoneInList(parentId, bone.Children);
                if (res != null)
                    return res;
            }

            return null;
        }
    }
}
