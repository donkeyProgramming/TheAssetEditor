using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Shared.Ui.Editors.BoneMapping
{
    [DebuggerDisplay("AnimatedBone - {Name} -> {MappedBoneName}")]
    public class AnimatedBone : NotifyPropertyChangedImpl
    {
        public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);
        public NotifyAttr<string> Name { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<int> BoneIndex { get; set; } = new NotifyAttr<int>(-1);
        public ObservableCollection<AnimatedBone> Children { get; set; } = new ObservableCollection<AnimatedBone>();

        public NotifyAttr<bool> IsUsedByCurrentModel { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<string> MappedBoneName { get; set; } = new NotifyAttr<string>(null);
        public NotifyAttr<int> MappedBoneIndex { get; set; } = new NotifyAttr<int>(-1);

        // Meta data
        public Vector3 BonePosOffset { get; set; } = new Vector3(0);
        public Vector3 BoneRotOffset { get; set; } = new Vector3(0);
        public float BoneScaleOffset { get; set; } = 1;


        public AnimatedBone()
        { }

        public AnimatedBone(int index = 0, string name = "")
        {
            BoneIndex.Value = index;
            Name.Value = name;
        }

        public void ClearMapping(bool includeChildren = true)
        {
            MappedBoneName.Value = "";
            MappedBoneIndex.Value = -1;

            if (includeChildren)
            {
                foreach (var child in Children)
                    child.ClearMapping(includeChildren);
            }
        }


        public void ApplySelfToChildren()
        {
            foreach (var child in Children)
            {
                child.MappedBoneName.Value = MappedBoneName.Value;
                child.MappedBoneIndex.Value = MappedBoneIndex.Value;
                child.ApplySelfToChildren();
            }
        }

        public AnimatedBone GetFromBoneId(int i)
        {
            if (BoneIndex.Value == i)
                return this;

            foreach (var child in Children)
            {
                var res = child.GetFromBoneId(i);
                if (res != null)
                    return res;
            }

            return null;
        }
    }
}
