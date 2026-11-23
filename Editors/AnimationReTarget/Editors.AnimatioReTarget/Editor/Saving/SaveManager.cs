using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.Settings;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace Editors.AnimatioReTarget.Editor.Saving
{
    public partial class SaveManager : ObservableObject
    {
        private readonly BoneManager _boneManager;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly IPackFileService _pfs;
        private readonly IFileSaveService _saveService;
        private readonly IStandardDialogs _standardDialogs;
        private readonly LocalizationManager _loc;
        private readonly IAbstractFormFactory<SaveWindow> _settingsWindowFactory;
        [ObservableProperty] SaveSettings _settings;

        SceneObject _generated;
        SceneObject _source;
        SceneObject _target;
        AnimationGenerationSettings _generationSettings;

        public SaveManager(BoneManager boneManager,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            IPackFileService pfs,
            SaveSettings saveSettings,
            IFileSaveService saveService,
            IStandardDialogs standardDialogs,
            LocalizationManager localizationManager,
            IAbstractFormFactory<SaveWindow> settingsWindowFactory)
        {
            _boneManager = boneManager;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _pfs = pfs;
            _saveService = saveService;
            _standardDialogs = standardDialogs;
            _loc = localizationManager;
            _settingsWindowFactory = settingsWindowFactory;
            _settings = saveSettings;
        }

        public void SetSceneNodes(SceneObject source, SceneObject target, SceneObject generated, AnimationGenerationSettings generationSettings)
        {
            _generated = generated;
            _source = source;
            _target = target;
            _generationSettings = generationSettings;
        }

        public void SaveAnimation(bool prompOnConflict = true)
        {
            if (_generated?.AnimationClip == null)
            {
                _standardDialogs.ShowDialogBox("Generated skeleton not set, or animation not created");
                return;
            }

            var animationName = _source.AnimationName.Value;
            var sourceFile = Settings.AnimationFormat == 8 ? _pfs.FindFile(animationName) : null;
            var sourceHeader = sourceFile == null ? null : AnimationFile.Create(sourceFile).Header;   // the header alone can run past what GetAnimationHeader peeks
            var animFile = ToOutputFile(_generated.AnimationClip, _generated.Skeleton, sourceHeader);

            _saveService.Save(BuildSourcePathOutput(animationName), AnimationFile.ConvertToBytes(animFile), prompOnConflict);
        }

        // The retargeted clip is on another skeleton, so only the source's header values carry over to v8 (flags,
        // unknown_v8), never its packing
        AnimationFile ToOutputFile(AnimationClip clip, GameSkeleton skeleton, AnimationFile.AnimationHeader? sourceHeader)
        {
            if (Settings.AnimationFormat == 8)
            {
                var unknownValueV8 = sourceHeader?.Version == 8 ? sourceHeader.UnknownValue_v8 : (uint?)null;
                return clip.ConvertToFileFormat(skeleton, 8, unknownValueV8, sourceHeader?.FlagVariables);
            }

            var animFile = clip.ConvertToFileFormat(skeleton);
            if (Settings.AnimationFormat != 7)
            {
                var skeletonFile = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(animFile.Header.SkeletonName);
                animFile.ConvertToVersion(Settings.AnimationFormat, skeletonFile, _pfs);
            }
            return animFile;
        }

        string BuildSourcePathOutput(string sourcePath)
        {
            var outputPath = sourcePath.Replace(_source.Skeleton.SkeletonName, _target.Skeleton.SkeletonName);
            var currentFileName = Path.GetFileName(outputPath);
            outputPath = outputPath.Replace(currentFileName, Settings.SavePrefix + currentFileName);
            return SaveUtility.EnsureEnding(outputPath, ".anim");
        }

        [RelayCommand] void ShowSaveSettings()
        {
            var window = _settingsWindowFactory.Create();
            window.Initialize(this);
            window.ShowDialog();
        }

        // Batch retarget. Ported from Szy-Cathay/TheAssetEditor-CN 08d0ed5dc, writing to the editable pack instead
        // of the fork's folder project

        public bool CanBatchRetarget(out string errorMessage)
        {
            if (_source?.Skeleton == null || _target?.Skeleton == null)
                errorMessage = _loc.Get("AnimationReTarget.Batch.Error.SkeletonsRequired");
            else if (!float.IsFinite(_generationSettings.AnimationSpeedMult) || _generationSettings.AnimationSpeedMult <= 0)
                errorMessage = _loc.Get("AnimationReTarget.Batch.Error.InvalidSpeed");
            else if (!_boneManager.FlatBones.Any(x => x.HasMapping && x.MappedIndex >= 0))
                errorMessage = _loc.Get("AnimationReTarget.Batch.Error.MappingRequired");
            else if (_pfs.GetEditablePack() == null)
                errorMessage = _loc.Get("AnimationReTarget.Batch.Error.EditablePackRequired");
            else
                errorMessage = string.Empty;

            return errorMessage.Length == 0;
        }

        /// <summary>The source skeleton's animations, without the skeleton files under animations\skeletons\.</summary>
        public IReadOnlyList<AnimationReference> GetBatchCandidates()
        {
            if (_source?.Skeleton == null)
                return [];

            return _skeletonAnimationLookUpHelper
                .GetAnimationsForSkeleton(_source.Skeleton.SkeletonName)
                .Where(x => !IsSkeletonAnimationPath(x.AnimationFile))
                .OrderBy(x => x.AnimationFile, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Container?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool IsTechAnimation(AnimationReference animation) =>
            GetPathSegments(animation.AnimationFile).Contains("tech", StringComparer.OrdinalIgnoreCase);

        public static bool IsSkeletonAnimationPath(string path)
        {
            if (!path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                return false;

            var segments = GetPathSegments(path);
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i].Equals("animations", StringComparison.OrdinalIgnoreCase) &&
                    segments[i + 1].Equals("skeletons", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static string[] GetPathSegments(string path) => path.Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);

        public Task<BatchAnimationRetargetResult> ExecuteBatchAsync(BatchAnimationRetargetRequest request, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
        {
            // Checked before the background task starts, so a bad setup throws to the caller and writes nothing
            if (!CanBatchRetarget(out var validationError))
                throw new InvalidOperationException(validationError);
            if (request.PathMode == BatchRetargetPathMode.SelectedFolder && string.IsNullOrWhiteSpace(request.TargetFolder))
                throw new InvalidOperationException(_loc.Get("AnimationReTarget.Batch.Error.TargetFolderRequired"));

            // The bone settings are read on the background task, so the window keeps them disabled until it ends
            return Task.Run(() => ExecuteBatch(request, progress, cancellationToken));
        }

        BatchAnimationRetargetResult ExecuteBatch(BatchAnimationRetargetRequest request, IProgress<BatchProgress>? progress, CancellationToken cancellationToken)
        {
            var outputPack = _pfs.GetEditablePack()!;

            // Plan every output path first. A path that collides with an earlier item, or that already exists when
            // not overwriting, is skipped
            var planned = request.Animations.Select(x => PlanItem(request, x)).ToList();
            var seenOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var skipped = new HashSet<int>();
            for (var i = 0; i < planned.Count; i++)
            {
                var outputPath = planned[i].OutputPath;
                if (outputPath == null)
                    continue;

                if (!seenOutputs.Add(outputPath) || (!request.OverwriteExisting && _pfs.FindFile(outputPath, outputPack) != null))
                    skipped.Add(i);
            }

            var results = new List<BatchAnimationRetargetItemResult>(planned.Count);
            for (var i = 0; i < planned.Count; i++)
            {
                var item = planned[i];
                var sourcePath = item.Animation.AnimationFile;

                if (cancellationToken.IsCancellationRequested)
                {
                    results.AddRange(planned.Skip(i).Select(x => new BatchAnimationRetargetItemResult(x.Animation.AnimationFile, x.OutputPath, BatchAnimationRetargetItemStatus.NotProcessed)));
                    break;
                }

                if (item.ErrorMessage != null)
                {
                    results.Add(new(sourcePath, null, BatchAnimationRetargetItemStatus.Failed, item.ErrorMessage));
                }
                else if (skipped.Contains(i))
                {
                    results.Add(new(sourcePath, item.OutputPath, BatchAnimationRetargetItemStatus.Skipped));
                }
                else
                {
                    try
                    {
                        var content = RetargetAnimation(item.Animation);
                        RunOnUiThread(() => _saveService.Save(item.OutputPath!, content, false));
                        results.Add(new(sourcePath, item.OutputPath, BatchAnimationRetargetItemStatus.Success));
                    }
                    catch (Exception e)
                    {
                        results.Add(new(sourcePath, item.OutputPath, BatchAnimationRetargetItemStatus.Failed, e.Message));
                    }
                }

                progress?.Report(new BatchProgress(i + 1, planned.Count, sourcePath));
            }

            return new BatchAnimationRetargetResult(results);
        }

        record BatchPlanItem(AnimationReference Animation, string? OutputPath, string? ErrorMessage);

        BatchPlanItem PlanItem(BatchAnimationRetargetRequest request, AnimationReference animation)
        {
            try
            {
                return new BatchPlanItem(animation, BuildBatchOutputPath(request, animation.AnimationFile), null);
            }
            catch (Exception e)
            {
                return new BatchPlanItem(animation, null, e.Message);
            }
        }

        string BuildBatchOutputPath(BatchAnimationRetargetRequest request, string sourcePath)
        {
            if (request.PathMode == BatchRetargetPathMode.SourcePath)
                return BuildSourcePathOutput(sourcePath);

            // Only the folder that holds the .anim is kept, so stand\idle.anim lands in <chosen>\stand\idle.anim
            var containingFolder = Path.GetFileName(Path.GetDirectoryName(sourcePath));
            if (string.IsNullOrWhiteSpace(containingFolder))
                throw new InvalidDataException(string.Format(_loc.Get("AnimationReTarget.Batch.Error.SourceFolderRequired"), sourcePath));

            var fileName = Settings.SavePrefix + Path.GetFileName(sourcePath);
            var targetFolder = request.TargetFolder!.Replace('/', '\\').Trim('\\');
            return SaveUtility.EnsureEnding(Path.Combine(targetFolder, containingFolder, fileName), ".anim");
        }

        byte[] RetargetAnimation(AnimationReference animation)
        {
            var packFile = _pfs.FindFile(animation.AnimationFile, animation.Container)
                ?? throw new FileNotFoundException(string.Format(_loc.Get("AnimationReTarget.Batch.Error.AnimationNotFound"), animation.AnimationFile));

            var sourceFile = AnimationFile.Create(packFile);
            var sourceClip = new AnimationClip(sourceFile, _source.Skeleton);
            var remapper = new AnimationRemapperService(_generationSettings, _boneManager.Bones);
            var remappedClip = remapper.ReMapAnimation(_source.Skeleton, _target.Skeleton, sourceClip);

            var outputFile = ToOutputFile(remappedClip, _target.Skeleton, sourceFile.Header);
            return AnimationFile.ConvertToBytes(outputFile);
        }

        // Adding to the pack raises events the pack tree handles, so writes go through the UI thread. Tests have no
        // Application and write directly
        static void RunOnUiThread(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }
    }
}
