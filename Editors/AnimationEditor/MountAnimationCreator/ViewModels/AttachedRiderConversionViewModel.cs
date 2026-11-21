using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AnimationEditor.MountAnimationCreator.Services;
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
    /// <summary>Converts the rider's animation between synchronized and attached (war beast) riding,
    /// see <see cref="RiderAttachmentConverter"/>. The result goes into the tool's preview rider.</summary>
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
        private readonly MountAnimationCreatorViewModel _saveSettings;

        GameSkeleton _attachSkeleton;
        RiderAttachmentConverter _converter;
        AnimationClip _converted;
        bool _previewIsAttached;

        public NotifyAttr<string> AttachSkeletonName { get; } = new("");
        public ObservableCollection<string> AttachBones { get; } = [];
        public NotifyAttr<string> SelectedAttachBone { get; }
        public NotifyAttr<bool> CanConvert { get; } = new(false);
        public NotifyAttr<bool> CanSave { get; } = new(false);

        public AttachedRiderConversionViewModel(SceneObject rider, SceneObject mount, SceneObject preview, SceneObjectEditor sceneObjectEditor,
            IStandardDialogs dialogs, IPackFileService pfs, IFileSaveService fileSaveService, ISkeletonAnimationLookUpHelper skeletonLookUp,
            MountAnimationCreatorViewModel saveSettings)
        {
            _rider = rider;
            _mount = mount;
            _preview = preview;
            _sceneObjectEditor = sceneObjectEditor;
            _dialogs = dialogs;
            _pfs = pfs;
            _fileSaveService = fileSaveService;
            _skeletonLookUp = skeletonLookUp;
            _saveSettings = saveSettings;

            SelectedAttachBone = new NotifyAttr<string>(null, _ => UpdateCanConvert());
            _rider.AnimationChanged += _ => UpdateCanConvert();
            _mount.AnimationChanged += _ => UpdateCanConvert();
            _preview.AnimationChanged += OnPreviewAnimationChanged;
            _preview.Player.OnFrameChanged += OnPreviewFrameChanged;
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
                _dialogs.ShowDialogBox($"{file.Name} has no ap_ bones. The attach bone is in the mount's tech skeleton, usually in a tech folder next to its meshes.", "No attach bones");
                return;
            }

            _attachSkeleton = skeleton;
            AttachSkeletonName.Value = _pfs.GetFullPath(file);
            AttachBones.Clear();
            foreach (var bone in attachBones)
                AttachBones.Add(bone);
            SelectedAttachBone.Value = attachBones.FirstOrDefault(x => x.Contains("rider", StringComparison.OrdinalIgnoreCase)) ?? attachBones[0];
        }

        void UpdateCanConvert()
        {
            CanConvert.Value = _attachSkeleton != null && SelectedAttachBone.Value != null &&
                _rider.AnimationClip != null && _rider.Skeleton != null &&
                _mount.AnimationClip != null && _mount.Skeleton != null;
        }

        public void ConvertRiderToAttached() => Convert(toAttached: true);
        public void ConvertRiderToSynchronized() => Convert(toAttached: false);

        void Convert(bool toAttached)
        {
            try
            {
                _converter = new RiderAttachmentConverter(_rider.Skeleton, _mount.Skeleton, _mount.AnimationClip, _attachSkeleton, _attachSkeleton.GetBoneIndexByName(SelectedAttachBone.Value));
            }
            catch (ArgumentException e)
            {
                _dialogs.ShowDialogBox(e.Message, "Tech skeleton doesn't fit the mount");
                return;
            }

            var converted = toAttached ? _converter.ToAttached(_rider.AnimationClip) : _converter.ToSynchronized(_rider.AnimationClip);

            _converted = converted;
            _previewIsAttached = toAttached;
            _sceneObjectEditor.CopyMeshFromOther(_preview, _rider);
            _sceneObjectEditor.SetAnimationClip(_preview, converted, toAttached ? "Converted to attached" : "Converted to synchronized");
            OnPreviewFrameChanged(_preview.Player.CurrentFrame);
            CanSave.Value = true;
        }

        // Anything else replacing the preview (the vertex preview, a new rider or mount animation) ends the conversion.
        void OnPreviewAnimationChanged(AnimationClip clip)
        {
            if (clip == _converted)
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

            var riderPath = _rider.AnimationName.Value;
            var prefix = _saveSettings.SavePrefixText.Value;
            var path = _saveSettings.EnsureUniqeFileName.Value
                ? MountAnimationGeneratorService.GenerateNewAnimationName(_pfs, riderPath, prefix)
                : Path.Combine(Path.GetDirectoryName(riderPath), prefix + Path.GetFileName(riderPath));

            var animFile = _converted.ConvertToFileFormat(_rider.Skeleton);
            var format = _saveSettings.SelectedAnimationOutputFormat.Value;
            if (format != 7)
                animFile.ConvertToVersion(format, _skeletonLookUp.GetSkeletonFileFromName(animFile.Header.SkeletonName), _pfs);

            _fileSaveService.Save(path, AnimationFile.ConvertToBytes(animFile), false);
        }
    }
}
