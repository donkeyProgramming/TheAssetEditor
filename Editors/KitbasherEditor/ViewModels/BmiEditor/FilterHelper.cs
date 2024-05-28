using System;
using System.Collections.ObjectModel;

namespace KitbasherEditor.ViewModels.BmiEditor
{
    class FilterHelper
    {
        public static ObservableCollection<SkeletonBoneNode> FilterBoneList(string filterText, bool onlySHowUsedBones, ObservableCollection<SkeletonBoneNode> completeList)
        {
            var output = new ObservableCollection<SkeletonBoneNode>();
            FilterBoneListRecursive(filterText, onlySHowUsedBones, completeList, output);
            return completeList;
        }

        static void FilterBoneListRecursive(string filterText, bool onlySHowUsedBones, ObservableCollection<SkeletonBoneNode> completeList, ObservableCollection<SkeletonBoneNode> output)
        {
            foreach (var item in completeList)
            {
                var isVisible = IsBoneVisibleInFilter(item, onlySHowUsedBones, filterText, true);
                item.IsVisible = isVisible;
                if (isVisible)
                {
                    FilterBoneListRecursive(filterText, onlySHowUsedBones, item.Children, item.Children);
                }
            }
        }

        static bool IsBoneVisibleInFilter(SkeletonBoneNode bone, bool onlySHowUsedBones, string filterText, bool checkChildren)
        {
            var contains = bone.BoneName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) != -1;
            if (onlySHowUsedBones)
            {
                if (contains && bone.IsUsedByCurrentModel)
                    return contains;
            }
            else
            {
                if (contains)
                    return contains;
            }
            if (checkChildren)
            {
                foreach (var child in bone.Children)
                {
                    var res = IsBoneVisibleInFilter(child, onlySHowUsedBones, filterText, checkChildren);
                    if (res == true)
                        return true;
                }
            }

            return false;
        }
    }
}
