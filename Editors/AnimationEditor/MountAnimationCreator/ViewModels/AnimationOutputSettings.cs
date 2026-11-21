using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.Animation;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    /// <summary>How the Rider Animation Editor names and writes the animations it makes.</summary>
    public class AnimationOutputSettings
    {
        public NotifyAttr<string> SavePrefix { get; } = new("new_");
        public NotifyAttr<bool> EnsureUniqueFileName { get; } = new(true);
        public ObservableCollection<uint> AnimationOutputFormats { get; } = [5, 6, 7];
        public NotifyAttr<uint> SelectedAnimationOutputFormat { get; } = new(7);

        /// <summary>Next to the source, with the prefix, numbered if taken and unique names are on. Paths in
        /// <paramref name="madeThisRun"/> are always numbered past, so a batch never overwrites its own output.</summary>
        public string OutputPath(IPackFileService pfs, string sourcePath, ISet<string> madeThisRun = null)
        {
            var directory = Path.GetDirectoryName(sourcePath);
            var name = Path.GetFileName(sourcePath);
            var path = Path.Combine(directory, SavePrefix.Value + name);
            for (var i = 1; (EnsureUniqueFileName.Value && pfs.FindFile(path) != null) || madeThisRun?.Contains(path) == true; i++)
                path = Path.Combine(directory, $"{SavePrefix.Value}{i}_{name}");
            return path;
        }

        /// <summary>Saves <paramref name="clip"/> next to <paramref name="sourcePath"/> and returns where it went.</summary>
        public string Save(IPackFileService pfs, IFileSaveService fileSaveService, ISkeletonAnimationLookUpHelper skeletonLookUp, AnimationClip clip, GameSkeleton skeleton, string sourcePath, ISet<string> madeThisRun = null)
        {
            var path = OutputPath(pfs, sourcePath, madeThisRun);
            var animFile = clip.ConvertToFileFormat(skeleton);
            var format = SelectedAnimationOutputFormat.Value;
            if (format != 7)
                animFile.ConvertToVersion(format, skeletonLookUp.GetSkeletonFileFromName(animFile.Header.SkeletonName), pfs);
            fileSaveService.Save(path, AnimationFile.ConvertToBytes(animFile), false);
            madeThisRun?.Add(path);
            return path;
        }
    }
}
