using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AnimationEditor.MountAnimationCreator.Services;
using Editors.AnimationVisualEditors.MountAnimationCreator.Services;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    /// <summary>The Attached tab: converts rider animations between synchronized and attached (war beast) riding,
    /// see <see cref="RiderAttachmentConverter"/>. A single conversion goes into the tool's preview rider.</summary>
    public class AttachedRiderConversionViewModel : NotifyPropertyChangedImpl
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
        private readonly MountLinkViewModel _sets;
        private readonly LocalizationManager _loc;

        GameSkeleton _attachSkeleton;
        RiderAttachmentConverter _converter;
        AnimationClip _converted;
        bool? _direction;   // the last conversion asked for: true to attached, false to synchronized
        bool _previewIsAttached;

        public NotifyAttr<string> AttachSkeletonName { get; } = new("");
        public ObservableCollection<string> AttachBones { get; } = [];
        public NotifyAttr<string> SelectedAttachBone { get; }
        public NotifyAttr<bool> CanConvert { get; } = new(false);
        public NotifyAttr<bool> CanSave { get; } = new(false);
        public NotifyAttr<bool> CanConvertSet { get; } = new(false);
        public NotifyAttr<string> ConvertStatus { get; } = new("");
        public NotifyAttr<string> ConvertSetStatus { get; } = new("");

        /// <summary>Set while the Attached tab is showing; the preview belongs to the other tab otherwise.</summary>
        public bool IsActive { get; set; }

        public AttachedRiderConversionViewModel(SceneObject rider, SceneObject mount, SceneObject preview, SceneObjectEditor sceneObjectEditor,
            IStandardDialogs dialogs, IPackFileService pfs, IFileSaveService fileSaveService, ISkeletonAnimationLookUpHelper skeletonLookUp,
            AnimationOutputSettings output, MountLinkViewModel sets, LocalizationManager loc)
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

            SelectedAttachBone = new NotifyAttr<string>(null, _ => OnSourceChanged());
            _preview.AnimationChanged += OnPreviewAnimationChanged;
            _preview.Player.OnFrameChanged += OnPreviewFrameChanged;
            UpdateStates();
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

            _attachSkeleton = skeleton;
            AttachSkeletonName.Value = _pfs.GetFullPath(file);
            AttachBones.Clear();
            foreach (var bone in attachBones)
                AttachBones.Add(bone);
            SelectedAttachBone.Value = attachBones.FirstOrDefault(x => x.Contains("rider", StringComparison.OrdinalIgnoreCase)) ?? attachBones[0];
        }

        public void UpdateStates()
        {
            var missing = new List<string>();
            if (_attachSkeleton == null || SelectedAttachBone.Value == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.TechSkeleton"));
            if (_rider.AnimationClip == null || _rider.Skeleton == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.RiderAnimation"));
            if (_mount.AnimationClip == null || _mount.Skeleton == null) missing.Add(_loc.Get("MountAnimationCreator.Needs.MountAnimation"));
            CanConvert.Value = missing.Count == 0;
            ConvertStatus.Value = Needs(missing);

            var setMissing = new List<string>();
            if (_attachSkeleton == null || SelectedAttachBone.Value == null) setMissing.Add(_loc.Get("MountAnimationCreator.Needs.TechSkeleton"));
            if (_sets.AnimationSetForRider.SelectedItem == null) setMissing.Add(_loc.Get("MountAnimationCreator.Needs.RiderSet"));
            if (_sets.AnimationSetForMount.SelectedItem == null) setMissing.Add(_loc.Get("MountAnimationCreator.Needs.MountSet"));
            CanConvertSet.Value = setMissing.Count == 0;
            ConvertSetStatus.Value = Needs(setMissing);
        }

        string Needs(List<string> missing) => missing.Count == 0 ? "" : _loc.Get("MountAnimationCreator.Needs") + " " + string.Join(", ", missing);

        public void ConvertRiderToAttached() => Convert(toAttached: true);
        public void ConvertRiderToSynchronized() => Convert(toAttached: false);

        /// <summary>A new rider or mount animation, or attach bone: the last conversion is redone on it.</summary>
        public void OnSourceChanged()
        {
            UpdateStates();
            if (!IsActive)
                return;
            if (_direction != null && CanConvert.Value)
                Convert(_direction.Value);
            else
                ClearPreview();
        }

        public void ClearPreview()
        {
            _direction = null;
            if (_preview.AnimationClip != null)
                _sceneObjectEditor.SetAnimation(_preview, null);
            OnPreviewAnimationChanged(null);
        }

        RiderAttachmentConverter CreateConverter(GameSkeleton riderSkeleton, GameSkeleton mountSkeleton, AnimationClip mountClip) =>
            new(riderSkeleton, mountSkeleton, mountClip, _attachSkeleton, _attachSkeleton.GetBoneIndexByName(SelectedAttachBone.Value));

        void Convert(bool toAttached)
        {
            UpdateStates();
            if (!CanConvert.Value)
                return;

            try
            {
                _converter = CreateConverter(_rider.Skeleton, _mount.Skeleton, _mount.AnimationClip);
            }
            catch (ArgumentException e)
            {
                _direction = null;
                _dialogs.ShowDialogBox(e.Message, _loc.Get("MountAnimationCreator.AttachedRiders.WrongTechSkeleton"));
                return;
            }

            _direction = toAttached;
            _converted = toAttached ? _converter.ToAttached(_rider.AnimationClip) : _converter.ToSynchronized(_rider.AnimationClip);
            _previewIsAttached = toAttached;
            _sceneObjectEditor.CopyMeshFromOther(_preview, _rider);
            _sceneObjectEditor.SetAnimationClip(_preview, _converted, toAttached ? "Converted to attached" : "Converted to synchronized");
            OnPreviewFrameChanged(_preview.Player.CurrentFrame);
            CanSave.Value = true;
        }

        // Anything else replacing the preview ends the conversion.
        void OnPreviewAnimationChanged(AnimationClip clip)
        {
            if (clip != null && clip == _converted)
                return;
            _converted = null;
            _previewIsAttached = false;
            _preview.Offset = Matrix.Identity;
            CanSave.Value = false;
        }

        // An attached rider is shown where the game puts it: on the attach bone.
        void OnPreviewFrameChanged(int frame)
        {
            _preview.Offset = _previewIsAttached && _converted != null
                ? _converter.AttachPointWorld(frame, _converted.DynamicFrames.Count)
                : Matrix.Identity;
        }

        public void SaveConvertedAnimation()
        {
            if (_converted == null || string.IsNullOrWhiteSpace(_rider.AnimationName.Value))
                return;
            _output.Save(_pfs, _fileSaveService, _skeletonLookUp, _converted, _rider.Skeleton, _rider.AnimationName.Value);
        }

        public void ConvertSetToAttached() => ConvertSet(toAttached: true);
        public void ConvertSetToSynchronized() => ConvertSet(toAttached: false);

        void ConvertSet(bool toAttached)
        {
            UpdateStates();
            if (!CanConvertSet.Value)
                return;

            var riderSet = _sets.AnimationSetForRider.SelectedItem;
            var mountSet = _sets.AnimationSetForMount.SelectedItem;
            var name = _dialogs.ShowTextInputDialog(_loc.Get("MountAnimationCreator.Batch.NameTitle"), _output.SavePrefix.Value + riderSet.Name);
            if (name == null || !name.Result || string.IsNullOrWhiteSpace(name.Text))
                return;

            var service = new RiderSetBatchService(_pfs, _fileSaveService, _skeletonLookUp, _output);
            var log = service.Process(riderSet, mountSet, name.Text.Trim(), slot => ConvertSlot(slot, toAttached));
            _dialogs.ShowErrorViewDialog(_loc.Get("MountAnimationCreator.Batch.ResultTitle"), log, false);
        }

        RiderSlotResult ConvertSlot(RiderSlotAnimation slot, bool toAttached)
        {
            var isAttached = RiderAttachmentConverter.IsAttached(slot.Rider);
            if (toAttached && isAttached)
                return RiderSlotResult.Keep("already attached");
            if (!toAttached && !isAttached)
                return RiderSlotResult.Keep("already synchronized");
            if (toAttached && RiderAttachmentConverter.StaysSynchronized(slot.RiderSlot))
                return RiderSlotResult.Keep("vanilla attached sets keep this slot synchronized");

            var converter = CreateConverter(slot.RiderSkeleton, slot.MountSkeleton, slot.Mount);
            return new RiderSlotResult(toAttached ? converter.ToAttached(slot.Rider) : converter.ToSynchronized(slot.Rider), "");
        }
    }
}
