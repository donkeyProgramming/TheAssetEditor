using System.Text.RegularExpressions;
using System.Windows;
using Shared.Core.Misc;
using Shared.Ui.Common;

namespace Shared.Ui.Editors.BoneMapping
{
    public class BoneMappingViewModel : NotifyPropertyChangedImpl
    {
       // protected IAssetEditorWindow _parentWindow;
        protected RemappedAnimatedBoneConfiguration _configuration;

        public FilterCollection<AnimatedBone> MeshBones { get; set; }
        public FilterCollection<AnimatedBone> ParentModelBones { get; set; }

        public NotifyAttr<bool> OnlyShowUsedBones { get; set; }
        public NotifyAttr<string> MeshSkeletonName { get; set; }
        public NotifyAttr<string> ParentSkeletonName { get; set; }
        public NotifyAttr<bool> ShowTransformSection { get; set; } = new NotifyAttr<bool>(false);
        public NotifyAttr<bool> ShowApplyButton { get; set; } = new NotifyAttr<bool>(false);

        public BoneMappingViewModel()
        {
            MeshBones = new FilterCollection<AnimatedBone>(null, OnBoneSelected);
            ParentModelBones = new FilterCollection<AnimatedBone>(null, OnParentBoneSelected);
            OnlyShowUsedBones = new NotifyAttr<bool>(true, (x) => MeshBones.RefreshFilter());
        }

        public void Initialize(/*IAssetEditorWindow parentWindow,*/ RemappedAnimatedBoneConfiguration configuration)
        {
            //_parentWindow = parentWindow;
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
            MappingUpdated();
        }

        public virtual void AutoMapSelfAndChildrenByName()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnNames(MeshBones.SelectedItem, ParentModelBones.PossibleValues);
            MappingUpdated();
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
            MappingUpdated();
        }

        public virtual void ClearBindingSelf()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ClearMapping(false);
            MappingUpdated();
        }

        public virtual void CopyMappingToAllChildren()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ApplySelfToChildren();
            MappingUpdated();
        }

        private void OnParentBoneSelected(AnimatedBone bone)
        {
            if (bone == null)
                return;

            MeshBones.SelectedItem.MappedBoneIndex.Value = bone.BoneIndex.Value;
            MeshBones.SelectedItem.MappedBoneName.Value = bone.Name.Value;

            if (_configuration.SkeletonBoneHighlighter != null)
                _configuration.SkeletonBoneHighlighter.SelectTargetSkeletonBone(bone.BoneIndex.Value);
            MappingUpdated();
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

        protected virtual void MappingUpdated()
        { }

        public void OnApplyButton()
        {
            ApplyChanges();
        }

        public bool OnOkButton()
        {
            var res = Validate(out var errorText);
            if (res == false)
            {
                var messageBoxResult = MessageBox.Show("Are you sure you want to do this?\n\n" + errorText + "\n\nContinue?", "Error", MessageBoxButton.OKCancel);
                if (messageBoxResult == MessageBoxResult.Cancel)
                    return false;
            }

            ApplyChanges();
            //_parentWindow.CloseWindow();
            return true;
        }

        public void OnCancelButton()
        {
           // _parentWindow.CloseWindow();
        }

        protected virtual void ApplyChanges()
        {
        }

        public virtual bool Validate(out string errorText)
        {
            errorText = "";
            return true;
        }
    }
}
