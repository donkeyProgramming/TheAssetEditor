using System;
using System.Collections.Generic;
using AnimationEditor.MountAnimationCreator.Services;
using Editors.AnimationVisualEditors.MountAnimationCreator.Services;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    /// <summary>The Attached Mount Change tab: moves an attached rider set made for one mount onto another, still attached.
    /// Attached animations sit on the attach bone, so they carry over unless they're repeated, stretched or offset;
    /// the synchronized slots of the set (deaths and kills) are copied unchanged.</summary>
    public class AttachedMountChangeViewModel
    {
        private readonly SceneObject _rider;
        private readonly SceneObject _mount;
        private readonly SceneObject _preview;
        private readonly SceneObjectEditor _sceneObjectEditor;
        private readonly IStandardDialogs _dialogs;
        private readonly IPackFileService _pfs;
        private readonly IFileSaveService _fileSaveService;
        private readonly ISkeletonAnimationLookUpHelper _skeletonLookUp;
        private readonly AnimationOutputSettings _output;
        private readonly RiderAndMountSets _sets;
        private readonly LocalizationManager _loc;

        AnimationClip _result;
        RiderAttachmentConverter _attachPoint;

        public AttachPointViewModel AttachPoint { get; }
        public AnimationSettingsViewModel Settings { get; } = new();

        public NotifyAttr<bool> CanSave { get; } = new(false);
        public NotifyAttr<bool> CanRefitSet { get; } = new(false);
        public NotifyAttr<string> SlotStatus { get; } = new("");
        public NotifyAttr<string> SetStatus { get; } = new("");
        public NotifyAttr<string> PreviewStatus { get; } = new("");

        /// <summary>Set while this tab is showing; the preview belongs to another tab otherwise.</summary>
        public bool IsActive { get; set; }

        public AttachedMountChangeViewModel(SceneObject rider, SceneObject mount, SceneObject preview, SceneObjectEditor sceneObjectEditor,
            IStandardDialogs dialogs, IPackFileService pfs, IFileSaveService fileSaveService, ISkeletonAnimationLookUpHelper skeletonLookUp,
            AnimationOutputSettings output, RiderAndMountSets sets, AttachPointViewModel attachPoint, LocalizationManager loc)
        {
            _rider = rider;
            _mount = mount;
            _preview = preview;
            _sceneObjectEditor = sceneObjectEditor;
            _dialogs = dialogs;
            _pfs = pfs;
            _fileSaveService = fileSaveService;
            _skeletonLookUp = skeletonLookUp;
            _output = output;
            _sets = sets;
            _loc = loc;
            AttachPoint = attachPoint;

            AttachPoint.Changed += OnSourceChanged;
            Settings.SettingsChanged += OnSourceChanged;
            _preview.AnimationChanged += OnPreviewAnimationChanged;
            _preview.Player.OnFrameChanged += OnPreviewFrameChanged;
            UpdateStates();
        }

        AttachedRiderRefit Refit => new(Settings.FitAnimation, (int)Settings.LoopCounter.Value, Settings.Translation.GetAsVector3(), Settings.Rotation.GetAsVector3());

        public void UpdateStates()
        {
            var missing = new List<string>();
            if (_rider.AnimationClip == null || _rider.Skeleton == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.RiderAnimation"));
            if (_mount.AnimationClip == null || _mount.Skeleton == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.MountAnimation"));
            CanSave.Value = missing.Count == 0;
            SlotStatus.Value = Needs(missing);

            var setMissing = new List<string>();
            if (_sets.RiderSet == null) setMissing.Add(_loc.Get("MountAnimationCreator.Needs.RiderSet"));
            if (_sets.MountSet == null) setMissing.Add(_loc.Get("MountAnimationCreator.Needs.MountSet"));
            CanRefitSet.Value = setMissing.Count == 0;
            SetStatus.Value = Needs(setMissing);
        }

        string Needs(List<string> missing) => missing.Count == 0 ? "" : _loc.Get("MountAnimationCreator.Needs") + " " + string.Join(", ", missing);

        /// <summary>Remakes the preview from the picked slots and the settings.</summary>
        public void OnSourceChanged()
        {
            UpdateStates();
            if (!IsActive)
                return;

            if (!CanSave.Value)
            {
                if (_preview.AnimationClip != null)
                    _sceneObjectEditor.SetAnimation(_preview, null);
                OnPreviewAnimationChanged(null);
                return;
            }

            _attachPoint = null;
            PreviewStatus.Value = _loc.Get("MountAnimationCreator.AttachedMountChange.NoTechSkeleton");
            if (AttachPoint.IsSet)
            {
                try
                {
                    _attachPoint = AttachPoint.CreateConverter(_rider.Skeleton, _mount.Skeleton, _mount.AnimationClip);
                    PreviewStatus.Value = "";
                }
                catch (ArgumentException e)
                {
                    PreviewStatus.Value = _loc.Get("MountAnimationCreator.AttachedRiders.WrongTechSkeleton") + ": " + e.Message;
                }
            }

            var result = Refit.Apply(_rider.AnimationClip, _mount.AnimationClip, _rider.Skeleton);
            _sceneObjectEditor.CopyMeshFromOther(_preview, _rider);
            _result = result;
            _sceneObjectEditor.SetAnimationClip(_preview, result, "On the new mount");
            OnPreviewFrameChanged(_preview.Player.CurrentFrame);
        }

        // Anything else replacing the preview ends this tab's preview
        void OnPreviewAnimationChanged(AnimationClip clip)
        {
            if (clip != null && clip == _result)
                return;
            _result = null;
        }

        // An attached rider is shown where the game puts it, on the new mount's attach bone. A synchronized one (a death,
        // say) already carries its place on the mount.
        void OnPreviewFrameChanged(int frame)
        {
            if (!IsActive)
                return;
            _preview.Offset = _result != null && _attachPoint != null && RiderAttachmentConverter.IsAttached(_result)
                ? _attachPoint.AttachPointWorld(frame, _result.DynamicFrames.Count)
                : Matrix.Identity;
        }

        public void SaveSlot()
        {
            UpdateStates();
            if (!CanSave.Value || string.IsNullOrWhiteSpace(_rider.AnimationName.Value))
                return;
            var result = Refit.Apply(_rider.AnimationClip, _mount.AnimationClip, _rider.Skeleton);
            _output.Save(_pfs, _fileSaveService, _skeletonLookUp, result, _rider.Skeleton, _rider.AnimationName.Value);
        }

        public void RefitSet()
        {
            UpdateStates();
            if (!CanRefitSet.Value)
                return;

            var riderSet = _sets.RiderSet;
            var mountSet = _sets.MountSet;
            var name = _dialogs.ShowTextInputDialog(_loc.Get("MountAnimationCreator.Batch.NameTitle"), _output.SavePrefix.Value + riderSet.Name);
            if (name == null || !name.Result || string.IsNullOrWhiteSpace(name.Text))
                return;

            var refit = Refit;
            var service = new RiderSetBatchService(_pfs, _fileSaveService, _skeletonLookUp, _output);
            var log = service.Process(riderSet, mountSet, name.Text.Trim(), slot =>
            {
                if (!RiderAttachmentConverter.IsAttached(slot.Rider))
                    return RiderSlotResult.Keep("synchronized, copied unchanged");
                if (!refit.ChangesAnything(slot.Rider, slot.Mount))
                    return RiderSlotResult.Keep("attached, fits the new mount as it is");
                return new RiderSlotResult(refit.Apply(slot.Rider, slot.Mount, slot.RiderSkeleton), "");
            });
            _dialogs.ShowErrorViewDialog(_loc.Get("MountAnimationCreator.Batch.ResultTitle"), log, false);
        }
    }
}
