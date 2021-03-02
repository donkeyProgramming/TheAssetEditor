using Common;
using Filetypes.RigidModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using View3D.Rendering.Geometry;

namespace KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping
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

        public List<IndexRemapping> BuildRemappingList()
        {
            List<IndexRemapping> output = new List<IndexRemapping>();
            RecusrivlyBuildMappingList(this, output);
            return output;
        }

        static void RecusrivlyBuildMappingList(AnimatedBone bone, List<IndexRemapping> output)
        {
            if (bone.MappedBoneIndex != -1)
            {
                var mapping = new IndexRemapping((byte)bone.BoneIndex, (byte)bone.MappedBoneIndex);
                output.Add(mapping);
            }

            foreach (var child in bone.Children)
                RecusrivlyBuildMappingList(child, output);
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

        public override string ToString()
        {
            return Name + " -> " + MappedBoneName;
        }
    }
}
