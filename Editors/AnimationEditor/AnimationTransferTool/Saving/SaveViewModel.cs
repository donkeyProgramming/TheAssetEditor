using System.Windows;
using AnimationEditor.AnimationTransferTool;
using CommonControls.SelectionListDialog;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.AnimationVisualEditors.AnimationTransferTool.BoneHandling;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.SelectionListDialog;
using Shared.Ui.Common;

namespace Editors.AnimationVisualEditors.AnimationTransferTool.Saving
{
    public partial class SaveViewModel : ObservableObject
    {
        //private readonly ILogger _logger = Logging.Create<SaveViewModel>();

        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IPackFileService _pfs;

        //private SceneObject _copyTo;
        //private SceneObject _copyFrom;
        //private SceneObject Generated { get; set; }
        //
        //
        //
        ////
        //private readonly AnimationGenerationSettings AnimationSettings;
        //private readonly BoneManager _boneManager;

        public SaveViewModel(BoneManager boneManager, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, IPackFileService pfs)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _pfs = pfs;
        }
        /*
        public void OpenBatchProcessDialog()
        {
            if (!CanUpdateAnimation(false))
                return;

            // Find all animations for skeleton
            var copyFromAnims = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(_copyFrom.Skeleton.SkeletonName);

            var items = copyFromAnims.Select(x => new SelectionListViewModel<SkeletonAnimationLookUpHelper.AnimationReference>.Item()
            {
                IsChecked = new NotifyAttr<bool>(!(x.AnimationFile.Contains("tech", StringComparison.InvariantCultureIgnoreCase) || x.AnimationFile.Contains("skeletons", StringComparison.InvariantCultureIgnoreCase))),
                DisplayName = x.AnimationFile,
                ItemValue = x
            }).ToList();

            var window = SelectionListWindow.ShowDialog("Select animations:", items);
            if (window.Result)
            {
                using (var waitCursor = new WaitCursor())
                {
                    var index = 1;
                    var numItemsToProcess = items.Count(x => x.IsChecked.Value);
                    if (numItemsToProcess > 50)
                    {
                        var confirm = MessageBox.Show("about to process 50 or more items! continue milord?", "sire...", MessageBoxButton.YesNo);
                        if (confirm != MessageBoxResult.Yes) return;
                    }
                    foreach (var item in items)
                    {
                        if (item.IsChecked.Value)
                        {
                            var file = _pfs.FindFile(item.ItemValue.AnimationFile, item.ItemValue.Container);
                            var animFile = AnimationFile.Create(file.DataSource.ReadDataAsChunk());
                            var clip = new AnimationClip(animFile, _copyFrom.Skeleton);

                            _logger.Here().Information($"Processing animation {index} / {numItemsToProcess} - {item.DisplayName}");

                            var updatedClip = UpdateAnimation(clip, null);
                            SaveAnimation(updatedClip, item.ItemValue.AnimationFile, false);
                            index++;
                        }

                    }
                }
            }
        }

        public void UpdateAnimation()
        {
            if (CanUpdateAnimation(true))
            {
                var newAnimationClip = UpdateAnimation(_copyFrom.AnimationClip, _copyTo.AnimationClip);
                _assetViewModelBuilder.SetAnimationClip(Generated, newAnimationClip, new SkeletonAnimationLookUpHelper.AnimationReference("Generated animation", null));
                _player.SelectedMainAnimation = _player.PlayerItems.First(x => x.Asset == Generated);
            }
        }

        AnimationClip UpdateAnimation(AnimationClip animationToCopy, AnimationClip originalAnimation)
        {
            var service = new AnimationRemapperService(AnimationSettings, _remappingInformation, Bones);
            var newClip = service.ReMapAnimation(_copyFrom.Skeleton, _copyTo.Skeleton, animationToCopy);
            return newClip;
        }

        bool CanUpdateAnimation(bool requireAnimation)
        {
            if (_remappingInformation == null)
            {
                MessageBox.Show("No mapping created?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Missing a skeleton?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyFrom.AnimationClip == null && requireAnimation)
            {
                MessageBox.Show("No animation to copy selected", "Error", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        public void SaveAnimationAction()
        {
            if (Generated.AnimationClip == null || Generated.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Can not save, as no animation has been generated. Press the Apply button first", "Error", MessageBoxButton.OK);
                return;
            }

            SaveAnimation(Generated.AnimationClip, _copyFrom.AnimationName.Value.AnimationFile);
        }

        void SaveAnimation(AnimationClip clip, string animationName, bool prompOnOverride = true)
        {
            var animFile = clip.ConvertToFileFormat(_copyTo.Skeleton);
            if (AnimationSettings.UseScaledSkeletonName.Value)
                animFile.Header.SkeletonName = AnimationSettings.ScaledSkeletonName.Value;

            if (AnimationSettings.AnimationOutputFormat.Value != 7)
            {
                var skeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(animFile.Header.SkeletonName);
                animFile.ConvertToVersion(AnimationSettings.AnimationOutputFormat.Value, skeleton, _pfs);
            }

            if (AnimationSettings.UseScaledSkeletonName.Value)
                animFile.Header.SkeletonName = AnimationSettings.ScaledSkeletonName.Value;

            var orgSkeleton = _copyFrom.Skeleton.SkeletonName;
            var newSkeleton = _copyTo.Skeleton.SkeletonName;
            var newPath = animationName.Replace(orgSkeleton, newSkeleton);
            var currentFileName = Path.GetFileName(newPath);
            newPath = newPath.Replace(currentFileName, AnimationSettings.SavePrefix.Value + currentFileName);
            newPath = SaveUtility.EnsureEnding(newPath, ".anim");

            _packFileSaveService.Save(newPath, AnimationFile.ConvertToBytes(animFile), prompOnOverride);
        }*/


    }

    public partial class SaveSettings : ObservableObject
    {
        public List<uint> PossibleAnimationFormats = [5, 6, 7];

        [ObservableProperty] string _savePrefix = "prefix_";
        [ObservableProperty] uint _animationFormat = 7;
        [ObservableProperty] bool _useGeneratedSkeleton = false;
        [ObservableProperty] string _scaledSkeletonName = "";
    }
}
