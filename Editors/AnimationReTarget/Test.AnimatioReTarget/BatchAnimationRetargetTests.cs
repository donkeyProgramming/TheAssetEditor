using Editors.AnimatioReTarget.Editor;
using Editors.AnimatioReTarget.Editor.Saving;
using Editors.Shared.Core.Common;
using GameWorld.Core.Services;
using Shared.Core.Commands;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Test.TestingUtility.Shared;
using Test.TestingUtility.TestUtility;

namespace Test.AnimatioReTarget
{
    // Ported from Szy-Cathay/TheAssetEditor-CN 08d0ed5dc (BatchAnimationRetargetServiceTests). Output goes to the
    // editable pack rather than the fork's folder project
    public class BatchAnimationRetargetTests
    {
        const string SourceAnimationPath = "animations\\battle\\humanoid01\\2handed_hammer\\stand\\hu1_2hh_stand_idle_01.anim";
        const string BatchFolder = "animations\\battle\\batch-output";
        const string FolderOutputPath = "animations\\battle\\batch-output\\stand\\prefix_hu1_2hh_stand_idle_01.anim";

        readonly string _inputPackFile = PathHelper.GetDataFile("Karl_and_celestialgeneral.pack");

        static BatchAnimationRetargetRequest Request(IReadOnlyList<AnimationReference> animations, bool overwrite = false, BatchRetargetPathMode mode = BatchRetargetPathMode.SelectedFolder, string? folder = BatchFolder)
            => new(animations, mode, folder, overwrite);

        static BatchAnimationRetargetResult Run(BatchContext context, BatchAnimationRetargetRequest request, IProgress<BatchProgress>? progress = null, CancellationToken cancellationToken = default)
            => context.Editor.SaveManager.ExecuteBatchAsync(request, progress, cancellationToken).GetAwaiter().GetResult();

        [Test]
        public void SelectedFolder_KeepsTheContainingFolder()
        {
            var context = new BatchContext(_inputPackFile);

            var result = Run(context, Request([context.Source]));

            Assert.That(result.SuccessCount, Is.EqualTo(1), result.Items.Single().ErrorMessage);
            Assert.That(result.Items.Single().OutputPath, Is.EqualTo(FolderOutputPath));
            Assert.That(context.FindOutput(FolderOutputPath), Is.Not.Null);
        }

        [Test]
        public void SelectedFolder_AcceptsForwardSlashesAndATrailingSlash()
        {
            var context = new BatchContext(_inputPackFile);

            var result = Run(context, Request([context.Source], folder: "animations/battle/batch-output/"));

            Assert.That(result.Items.Single().OutputPath, Is.EqualTo(FolderOutputPath));
        }

        [Test]
        public void SourcePath_MatchesTheSingleSaveRule()
        {
            var context = new BatchContext(_inputPackFile);

            var result = Run(context, Request([context.Source], mode: BatchRetargetPathMode.SourcePath, folder: null));

            Assert.That(result.SuccessCount, Is.EqualTo(1), result.Items.Single().ErrorMessage);
            Assert.That(context.FindOutput("animations\\battle\\humanoid01e\\2handed_hammer\\stand\\prefix_hu1_2hh_stand_idle_01.anim"), Is.Not.Null);
        }

        [Test]
        public void ExistingOutput_IsSkippedByDefault()
        {
            var context = new BatchContext(_inputPackFile);
            byte[] existing = [1, 2, 3, 4];
            context.AddOutputFile(FolderOutputPath, existing);

            var result = Run(context, Request([context.Source]));

            Assert.That(result.SuccessCount, Is.Zero);
            Assert.That(result.SkippedCount, Is.EqualTo(1));
            Assert.That(context.FindOutput(FolderOutputPath)!.DataSource.ReadData(), Is.EqualTo(existing));
        }

        [Test]
        public void ExistingOutput_IsOverwrittenWhenAsked()
        {
            var context = new BatchContext(_inputPackFile);
            byte[] existing = [1, 2, 3, 4];
            context.AddOutputFile(FolderOutputPath, existing);

            var result = Run(context, Request([context.Source], overwrite: true));

            Assert.That(result.SuccessCount, Is.EqualTo(1), result.Items.Single().ErrorMessage);
            Assert.That(result.SkippedCount, Is.Zero);
            Assert.That(context.FindOutput(FolderOutputPath)!.DataSource.ReadData(), Is.Not.EqualTo(existing));
        }

        [Test]
        public void DuplicateOutputs_SkipTheLaterItemEvenWhenOverwriting()
        {
            var context = new BatchContext(_inputPackFile);

            var result = Run(context, Request([context.Source, context.Source], overwrite: true));

            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(result.SkippedCount, Is.EqualTo(1));
            Assert.That(result.Items[1].Status, Is.EqualTo(BatchAnimationRetargetItemStatus.Skipped));
        }

        [Test]
        public void MissingAnimation_FailsAndTheRestStillRun()
        {
            var context = new BatchContext(_inputPackFile);
            const string missing = "animations\\battle\\humanoid01\\2handed_hammer\\attacks\\missing.anim";

            var result = Run(context, Request([new AnimationReference(missing, context.Source.Container), context.Source]));

            Assert.That(result.FailureCount, Is.EqualTo(1));
            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(result.Items[0].SourcePath, Is.EqualTo(missing));
            Assert.That(context.FindOutput(FolderOutputPath), Is.Not.Null);
        }

        [Test]
        public void AnimationOutsideAFolder_FailsAndTheRestStillRun()
        {
            var context = new BatchContext(_inputPackFile);

            var result = Run(context, Request([new AnimationReference("orphan.anim", context.Source.Container), context.Source]));

            Assert.That(result.FailureCount, Is.EqualTo(1));
            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(result.Items[0].SourcePath, Is.EqualTo("orphan.anim"));
        }

        [Test]
        public void SelectedFolderWithoutAFolder_Throws()
        {
            var context = new BatchContext(_inputPackFile);

            Assert.That(() => Run(context, Request([context.Source], folder: " ")), Throws.InvalidOperationException);
            Assert.That(context.Output.GetFileCount(), Is.Zero);
        }

        [Test]
        public void CancelAfterTheFirstItem_ReportsTheRestAsNotProcessed()
        {
            var context = new BatchContext(_inputPackFile);
            using var cancellation = new CancellationTokenSource();
            var progress = new InlineProgress<BatchProgress>(x =>
            {
                if (x.Completed == 1)
                    cancellation.Cancel();
            });

            var result = Run(context, Request([context.Source, context.Source], overwrite: true), progress, cancellation.Token);

            Assert.That(result.SuccessCount, Is.EqualTo(1));
            Assert.That(result.NotProcessedCount, Is.EqualTo(1));
            Assert.That(context.FindOutput(FolderOutputPath), Is.Not.Null);
        }

        [Test]
        public void NoBoneMapped_ThrowsAndWritesNothing()
        {
            var context = new BatchContext(_inputPackFile);
            foreach (var bone in context.Editor.BoneManager.FlatBones)
            {
                bone.HasMapping = false;
                bone.MappedIndex = -1;
            }

            Assert.That(() => Run(context, Request([context.Source])), Throws.InvalidOperationException);
            Assert.That(context.Output.GetFileCount(), Is.Zero);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void InvalidSpeed_ThrowsAndWritesNothing(float speed)
        {
            var context = new BatchContext(_inputPackFile);
            context.Editor.Settings.AnimationSpeedMult = speed;

            Assert.That(() => Run(context, Request([context.Source])), Throws.InvalidOperationException);
            Assert.That(context.Output.GetFileCount(), Is.Zero);
        }

        [TestCase(5u)]
        [TestCase(6u)]
        [TestCase(7u)]
        [TestCase(8u)]
        public void SavesInTheChosenFormat(uint format)
        {
            var context = new BatchContext(_inputPackFile);
            context.Editor.SaveManager.Settings.AnimationFormat = format;

            var result = Run(context, Request([context.Source]));

            Assert.That(result.SuccessCount, Is.EqualTo(1), result.Items.Single().ErrorMessage);
            var saved = AnimationFile.Create(context.FindOutput(FolderOutputPath)!);
            Assert.That(saved.Header.Version, Is.EqualTo(format));
            if (format == 8)
                Assert.That(saved.Header.UnknownValue_v8, Is.EqualTo(6));
        }

        [Test]
        public void Candidates_LeaveOutSkeletonFiles()
        {
            var context = new BatchContext(_inputPackFile);

            var candidates = context.Editor.SaveManager.GetBatchCandidates().Select(x => x.AnimationFile).ToList();

            Assert.That(candidates, Does.Contain(SourceAnimationPath));
            Assert.That(candidates, Has.None.Matches<string>(SaveManager.IsSkeletonAnimationPath));
        }

        [Test]
        public void IsSkeletonAnimationPath_MatchesOnlyAnimationsSkeletons()
        {
            Assert.That(SaveManager.IsSkeletonAnimationPath("animations\\skeletons\\humanoid01.anim"), Is.True);
            Assert.That(SaveManager.IsSkeletonAnimationPath("animations/skeletons/humanoid01.anim"), Is.True);
            Assert.That(SaveManager.IsSkeletonAnimationPath("animations\\battle\\humanoid16\\skeletons\\idle.anim"), Is.False);
            Assert.That(SaveManager.IsSkeletonAnimationPath("animations\\skeletons\\humanoid01.bone_inv_trans_mats"), Is.False);
        }

        [Test]
        public void IsTechAnimation_MatchesOnlyATechFolder()
        {
            Assert.That(SaveManager.IsTechAnimation(new AnimationReference("animations\\battle\\humanoid16\\tech\\test.anim", null!)), Is.True);
            Assert.That(SaveManager.IsTechAnimation(new AnimationReference("animations\\battle\\humanoid16\\attacks\\technique.anim", null!)), Is.False);
        }

        class BatchContext
        {
            public AnimationRetargetEditor Editor { get; }
            public IPackFileContainer Output { get; }
            public AnimationReference Source { get; }

            readonly IPackFileService _pfs;

            public BatchContext(string inputPackFile)
            {
                var runner = new AssetEditorTestRunner();
                runner.CreateCaContainer();
                Output = runner.LoadPackFile(inputPackFile, true)!;
                _pfs = runner.PackFileService;

                var sourceContainer = _pfs.GetAllPackfileContainers().Single(x => x != Output && _pfs.FindFile(SourceAnimationPath, x) != null);
                Source = new AnimationReference(SourceAnimationPath, sourceContainer);

                Editor = (AnimationRetargetEditor)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AnimationRetarget_Editor);
                var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
                var source = Editor.GetSceneObjectFromId(AnimationRetargetIds.Source)!.Data;
                var target = Editor.GetSceneObjectFromId(AnimationRetargetIds.Target)!.Data;
                sceneObjectEditor.SetMesh(source, _pfs.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel")!);
                sceneObjectEditor.SetAnimation(source, SourceAnimationPath);
                sceneObjectEditor.SetMesh(target, _pfs.FindFile(@"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_body_02.wsmodel")!);
                Editor.BoneManager.ApplyDefaultMapping();
            }

            public PackFile? FindOutput(string path) => _pfs.FindFile(path, Output);

            public void AddOutputFile(string path, byte[] content)
            {
                var packFile = new PackFile(Path.GetFileName(path), new MemorySource(content));
                _pfs.AddFilesToPack(Output, [new NewPackFileEntry(Path.GetDirectoryName(path)!, packFile)]);
            }
        }

        class InlineProgress<T>(Action<T> report) : IProgress<T>
        {
            public void Report(T value) => report(value);
        }
    }
}
