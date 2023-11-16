using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CommonControls.Common;

namespace CommonControls.Editors.BoneMapping
{
    public class BoneMappingViewModel : NotifyPropertyChangedImpl
    {
        protected RemappedAnimatedBoneConfiguration _configuration;

        public FilterCollection<AnimatedBone> MeshBones { get; set; }
        public FilterCollection<AnimatedBone> ParentModelBones { get; set; }

        public NotifyAttr<bool> OnlyShowUsedBones { get; set; }
        public NotifyAttr<string> MeshSkeletonName { get; set; }
        public NotifyAttr<string> ParentSkeletonName { get; set; }

        public BoneMappingViewModel()
        {
            MeshBones = new FilterCollection<AnimatedBone>(null, OnBoneSelected);
            ParentModelBones = new FilterCollection<AnimatedBone>(null, OnParentBoneSelected);
            OnlyShowUsedBones = new NotifyAttr<bool>(true, (x) => MeshBones.RefreshFilter());
        }

        public void BaseInitialize(RemappedAnimatedBoneConfiguration configuration)
        {
            _configuration = configuration;
            CreateFromConfiguration(_configuration);

            MeshBones.SelectedItem = MeshBones.Values.FirstOrDefault();
            MeshBones.SearchFilterExtended += FilterMeshBones;
            MeshBones.RefreshFilter();

            ParentModelBones.SearchFilterExtended += FilterMeshBones;
            ParentModelBones.RefreshFilter();
        }

        void CreateFromConfiguration(RemappedAnimatedBoneConfiguration config)
        {
            var boneSelector = _configuration?.SkeletonBoneHighlighter;
            _configuration = config;
            if (_configuration.SkeletonBoneHighlighter == null)
                _configuration.SkeletonBoneHighlighter = boneSelector;

            MeshBones.UpdatePossibleValues(config.MeshBones);
            MeshSkeletonName = new NotifyAttr<string>(config.MeshSkeletonName);

            ParentModelBones.UpdatePossibleValues(config.ParentModelBones);
            ParentSkeletonName = new NotifyAttr<string>(config.ParnetModelSkeletonName);
        }

        void FilterMeshBones(FilterCollection<AnimatedBone> value, Regex regex)
        {
            AnimatedBoneHelper.FilterBoneList(regex, OnlyShowUsedBones.Value, value.PossibleValues);
        }

        public virtual void ClearBindingSelfAndChildren()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ClearMapping();
        }

        public virtual void AutoMapSelfAndChildrenByName()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnNames(MeshBones.SelectedItem, ParentModelBones.PossibleValues);
        }

        public virtual void AutoMapSelfAndChildrenByHierarchy()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }
            if (ParentModelBones.SelectedItem == null)
            {
                MessageBox.Show("No parent model bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnHierarchy(MeshBones.SelectedItem, ParentModelBones.SelectedItem);
        }

        public virtual void ClearBindingSelf()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ClearMapping(false);
        }

        public virtual void CopyMappingToAllChildren()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ApplySelfToChildren();
        }

        private void OnParentBoneSelected(AnimatedBone bone)
        {
            if (bone == null)
                return;

            MeshBones.SelectedItem.MappedBoneIndex.Value = bone.BoneIndex.Value;
            MeshBones.SelectedItem.MappedBoneName.Value = bone.Name.Value;

            OnMappingCreated(MeshBones.SelectedItem.BoneIndex.Value, MeshBones.SelectedItem.MappedBoneIndex.Value);

            if (_configuration.SkeletonBoneHighlighter != null)
                _configuration.SkeletonBoneHighlighter.SelectTargetSkeletonBone(bone.BoneIndex.Value);
        }

        private void OnBoneSelected(AnimatedBone bone)
        {
            if (_configuration.SkeletonBoneHighlighter != null)
            {
                if (bone == null)
                    _configuration.SkeletonBoneHighlighter.SelectSourceSkeletonBone(-1);
                else
                {
                    _configuration.SkeletonBoneHighlighter.SelectSourceSkeletonBone(bone.BoneIndex.Value);
                    if (bone.MappedBoneIndex.Value != -1)
                        _configuration.SkeletonBoneHighlighter.SelectTargetSkeletonBone(bone.MappedBoneIndex.Value);
                }
            }
        }

        public virtual void OnMappingCreated(int originalBoneIndex, int newBoneIndex)
        { }

        public virtual bool Validate(out string errorText)
        {
            var usedBonesCount = AnimatedBoneHelper.GetUsedBonesCount(MeshBones.PossibleValues.First());
            var mapping = AnimatedBoneHelper.BuildRemappingList(MeshBones.PossibleValues.First());
            var numMappings = mapping.Count(x => x.IsUsedByModel);
            if (usedBonesCount != numMappings)
            {
                errorText = "Not all bones mapped. This will not work as you expect and will case problems later!\nOnly do this if your REALLY know what you are doing";
                return false;
            }
            errorText = "";
            return true;
        }
    }
}
