using System;
using System.Collections.ObjectModel;
using System.Linq;
using AnimationEditor.MountAnimationCreator.Services;
using GameWorld.Core.Animation;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    /// <summary>The mount's tech skeleton and the ap_ bone attached riders are glued to. Shared by the tabs that need it,
    /// so it's picked once.</summary>
    public class AttachPointViewModel
    {
        private readonly IStandardDialogs _dialogs;
        private readonly IPackFileService _pfs;
        private readonly LocalizationManager _loc;

        public event Action Changed;

        public GameSkeleton Skeleton { get; private set; }
        public NotifyAttr<string> AttachSkeletonName { get; } = new("");
        public ObservableCollection<string> AttachBones { get; } = [];
        public NotifyAttr<string> SelectedAttachBone { get; }

        public bool IsSet => Skeleton != null && SelectedAttachBone.Value != null;

        public AttachPointViewModel(IStandardDialogs dialogs, IPackFileService pfs, LocalizationManager loc)
        {
            _dialogs = dialogs;
            _pfs = pfs;
            _loc = loc;
            SelectedAttachBone = new NotifyAttr<string>(null, _ => Changed?.Invoke());
        }

        public void BrowseAttachSkeleton()
        {
            var result = _dialogs.DisplayBrowseDialog([".anim"], path => path.Contains(@"\tech\", StringComparison.OrdinalIgnoreCase));
            if (result.Result && result.File != null)
                SetAttachSkeleton(result.File);
        }

        public void SetAttachSkeleton(PackFile file)
        {
            var skeleton = new GameSkeleton(AnimationFile.Create(file), null);
            var attachBones = skeleton.BoneNames.Where(x => x.StartsWith("ap_", StringComparison.OrdinalIgnoreCase)).ToList();
            if (attachBones.Count == 0)
            {
                _dialogs.ShowDialogBox(string.Format(_loc.Get("MountAnimationCreator.AttachedRiders.NoAttachBones"), file.Name), _loc.Get("MountAnimationCreator.AttachedRiders.NoAttachBones.Title"));
                return;
            }

            Skeleton = skeleton;
            AttachSkeletonName.Value = _pfs.GetFullPath(file);
            AttachBones.Clear();
            foreach (var bone in attachBones)
                AttachBones.Add(bone);
            SelectedAttachBone.Value = attachBones.FirstOrDefault(x => x.Contains("rider", StringComparison.OrdinalIgnoreCase)) ?? attachBones[0];
        }

        /// <summary>Throws <see cref="ArgumentException"/> when the tech skeleton doesn't fit the mount skeleton.</summary>
        public RiderAttachmentConverter CreateConverter(GameSkeleton riderSkeleton, GameSkeleton mountSkeleton, AnimationClip mountClip) =>
            new(riderSkeleton, mountSkeleton, mountClip, Skeleton, Skeleton.GetBoneIndexByName(SelectedAttachBone.Value));
    }
}
