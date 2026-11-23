using GameWorld.Core.Services;

namespace Editors.AnimatioReTarget.Editor.Saving
{
    public enum BatchRetargetPathMode
    {
        /// <summary>The single-save rule: the source path with the source skeleton name swapped for the target's.</summary>
        SourcePath,

        /// <summary>The chosen folder, plus the folder that holds each source .anim.</summary>
        SelectedFolder,
    }

    public record BatchAnimationRetargetRequest(
        IReadOnlyList<AnimationReference> Animations,
        BatchRetargetPathMode PathMode,
        string? TargetFolder,
        bool OverwriteExisting);

    public enum BatchAnimationRetargetItemStatus
    {
        Success,
        Skipped,
        Failed,
        NotProcessed,
    }

    public record BatchAnimationRetargetItemResult(
        string SourcePath,
        string? OutputPath,
        BatchAnimationRetargetItemStatus Status,
        string? ErrorMessage = null);

    public record BatchProgress(int Completed, int Total, string CurrentFile);

    public class BatchAnimationRetargetResult(IReadOnlyList<BatchAnimationRetargetItemResult> items)
    {
        public IReadOnlyList<BatchAnimationRetargetItemResult> Items { get; } = items;
        public int SuccessCount => Count(BatchAnimationRetargetItemStatus.Success);
        public int SkippedCount => Count(BatchAnimationRetargetItemStatus.Skipped);
        public int FailureCount => Count(BatchAnimationRetargetItemStatus.Failed);
        public int NotProcessedCount => Count(BatchAnimationRetargetItemStatus.NotProcessed);

        int Count(BatchAnimationRetargetItemStatus status) => Items.Count(x => x.Status == status);
    }
}
