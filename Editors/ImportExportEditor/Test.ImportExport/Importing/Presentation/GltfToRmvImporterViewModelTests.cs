using Editors.ImportExport.Importing;
using Editors.ImportExport.Importing.Importers.GltfToRmv;
using Editors.ImportExport.Importing.Presentation;
using Editors.ImportExport.Misc;
using Editors.ImportImport.Importing.Presentation.RmvToGltf;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.Core.PackFiles.Models.Containers;
using Shared.Core.Services;
using Shared.Core.Settings;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Importing.Presentation;

public class GltfToRmvImporterViewModelTests
{
    [Test]
    public void AnimationSampling_DefaultsToAutoAndManualFieldTracksAvailability()
    {
        var viewModel = new RmvToGltfImporterViewModel(null!);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.AutoDetectAnimationKeysPerSecond, Is.True);
            Assert.That(viewModel.AutoScaleHumanoid, Is.True);
            Assert.That(
                viewModel.SourceForwardDirection,
                Is.EqualTo(GltfSourceForwardDirection.PositiveZ));
            Assert.That(
                viewModel.SourceForwardDirections.Select(option => option.Value),
                Is.EqualTo(new[]
                {
                    GltfSourceForwardDirection.PositiveZ,
                    GltfSourceForwardDirection.PositiveX,
                    GltfSourceForwardDirection.NegativeX,
                    GltfSourceForwardDirection.NegativeZ,
                }));
            Assert.That(
                viewModel.SourceForwardDirections.Select(option => option.Key),
                Is.EqualTo(new[]
                {
                    "+Z (standard glTF)",
                    "+X (Unreal/PSK)",
                    "-X",
                    "-Z",
                }));
            Assert.That(viewModel.CanEditAnimationKeysPerSecond, Is.False);
        });

        viewModel.AutoDetectAnimationKeysPerSecond = false;
        Assert.That(viewModel.CanEditAnimationKeysPerSecond, Is.True);

        viewModel.ImportAnimations = false;
        Assert.That(viewModel.CanEditAnimationKeysPerSecond, Is.False);
    }

    [Test]
    public void Initialize_StandardGlb_UsesSkinNameAsEditableSkeletonDefault()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var root = scene.CreateNode("root");
        modelRoot.CreateSkin("ExternalArmature").BindJoints(
            System.Numerics.Matrix4x4.Identity,
            root);
        var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(inputPath);
            var viewModel = new RmvToGltfImporterViewModel(null!);

            viewModel.Initialize(new PackFile(
                inputPath,
                new FileSystemSource(inputPath)));

            Assert.That(viewModel.NewSkeletonName, Is.EqualTo("ExternalArmature"));
            viewModel.NewSkeletonName = "CustomSkeleton";
            Assert.That(viewModel.NewSkeletonName, Is.EqualTo("CustomSkeleton"));
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [Test]
    public void Initialize_StandardGlbWithoutSkinSkeletonProperty_UsesArmatureName()
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("default");
        var armature = scene.CreateNode("ExternalArmature");
        var root = armature.CreateNode("root");
        var skin = modelRoot.CreateSkin();
        skin.BindJoints(System.Numerics.Matrix4x4.Identity, root);
        skin.Skeleton = null;
        var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            modelRoot.SaveGLB(inputPath);
            var viewModel = new RmvToGltfImporterViewModel(null!);

            viewModel.Initialize(new PackFile(
                inputPath,
                new FileSystemSource(inputPath)));

            Assert.That(viewModel.NewSkeletonName, Is.EqualTo("ExternalArmature"));
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [Test]
    public void Initialize_GltfWithCyclicNonJointAncestors_DoesNotHang()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.gltf");
        try
        {
            File.WriteAllText(inputPath, """
                {
                  "asset": { "version": "2.0" },
                  "nodes": [
                    { "children": [1] },
                    { "children": [0, 2] },
                    { "name": "root" }
                  ],
                  "skins": [{ "joints": [2] }]
                }
                """);
            var viewModel = new RmvToGltfImporterViewModel(null!);

            var initializeTask = Task.Run(() => viewModel.Initialize(new PackFile(
                inputPath,
                new FileSystemSource(inputPath))));

            Assert.That(initializeTask.Wait(TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(viewModel.NewSkeletonName, Is.EqualTo(Path.GetFileNameWithoutExtension(inputPath)));
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [Test]
    public async Task ImportAsync_ReturnsStructuredImporterResult()
    {
        var expected = ImportResult.Failure(["The pack already contains models\\test.rigid_model_v2"]);
        var importer = new TestImporter(expected);
        var viewModel = new ImporterCoreViewModel(
            [importer],
            new ApplicationSettingsService(GameTypeEnum.Warhammer3));
        var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
        try
        {
            File.WriteAllBytes(inputPath, []);
            viewModel.Initialize(
                PackFileContainer.CreatePackFile("test"),
                "models",
                inputPath);

            var actual = await viewModel.ImportAsync();

            Assert.That(actual, Is.SameAs(expected));
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [Test]
    public async Task ImportAsync_CancelCommandStopsCancellationAwareImporter()
    {
        var importer = new CancellationAwareImporter();
        var viewModel = new ImporterCoreViewModel(
            [importer],
            new ApplicationSettingsService(GameTypeEnum.Warhammer3));
        var inputPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.glb");
        try
        {
            File.WriteAllBytes(inputPath, []);
            viewModel.Initialize(
                PackFileContainer.CreatePackFile("test"),
                "models",
                inputPath);
            var importTask = viewModel.ImportAsync();
            Assert.That(importer.Started.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(viewModel.CancelImportCommand.CanExecute(null), Is.True);

            viewModel.CancelImportCommand.Execute(null);
            var result = await importTask;

            Assert.That(result, Is.Null);
            Assert.That(viewModel.IsOperationActive, Is.False);
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    [Test]
    public void BuildResultMessage_ListsStructuredOutputsWarningsAndErrors()
    {
        var successMessage = ImporterCoreViewModel.BuildResultMessage(new ImportResult(
            true,
            [@"models\test.rigid_model_v2"],
            ["test warning"],
            [],
            HumanoidScale: new HumanoidScaleImportSummary(
                true,
                4,
                2,
                0.5f,
                "Applied one scale factor."),
            MaterialSummary: new MaterialImportSummary(
                [new MaskedMaterialImportSummary("hair_mask", 0.7f)],
                [
                    new SkippedMaterialSemantic("decorated", "Emissive"),
                    new SkippedMaterialSemantic("decorated", "Occlusion"),
                ]),
            SourceForward: new SourceForwardImportSummary(
                "+X (Unreal/PSK)",
                "Rotated the source +X onto the game +Z front.")));
        var failureMessage = ImporterCoreViewModel.BuildResultMessage(
            ImportResult.Failure([
                "The pack already contains models\\test.rigid_model_v2",
                "The pack already contains models\\test.anim",
            ]));
        var disabledMessage = ImporterCoreViewModel.BuildResultMessage(new ImportResult(
            true,
            [],
            [],
            [],
            HumanoidScale: new HumanoidScaleImportSummary(
                false,
                null,
                null,
                1,
                "Not applied: automatic humanoid scaling is turned off.")));

        Assert.Multiple(() =>
        {
            Assert.That(successMessage, Does.Contain(@"models\test.rigid_model_v2"));
            Assert.That(successMessage, Does.Contain("test warning"));
            Assert.That(successMessage, Does.Contain("Source height: 4"));
            Assert.That(successMessage, Does.Contain("humanoid01 reference height: 2"));
            Assert.That(successMessage, Does.Contain("Scale factor: 0.5"));
            Assert.That(successMessage, Does.Contain("hair_mask"));
            Assert.That(successMessage, Does.Contain("cutoff 0.7"));
            Assert.That(successMessage, Does.Contain("emissive channel"));
            Assert.That(successMessage, Does.Contain("occlusion channel"));
            Assert.That(successMessage, Does.Contain("no reliable game equivalent"));
            Assert.That(successMessage, Does.Contain("Source front direction"));
            Assert.That(successMessage, Does.Contain("+X (Unreal/PSK)"));
            Assert.That(successMessage, Does.Contain("game +Z"));
            Assert.That(disabledMessage, Does.Contain("Source height: not measured"));
            Assert.That(disabledMessage, Does.Contain("humanoid01 reference height: not measured"));
            Assert.That(disabledMessage, Does.Contain("Scale factor: 1"));
            Assert.That(disabledMessage, Does.Contain("turned off"));
            Assert.That(failureMessage, Does.Contain(@"models\test.rigid_model_v2"));
            Assert.That(failureMessage, Does.Contain(@"models\test.anim"));
        });
    }

    private sealed class TestImporter(ImportResult result) : IImporterViewModel
    {
        public string DisplayName => "test";
        public string OutputExtension => ".rigid_model_v2";
        public string[] InputExtensions => [".glb"];

        public ImportResult Execute(
            PackFile exportSource,
            string outputPath,
            IPackFileContainer packFileContainer,
            GameTypeEnum gameType,
            IProgress<ImportProgress>? progress,
            CancellationToken cancellationToken) => result;

        public ImportSupportEnum CanImportFile(PackFile file) =>
            ImportSupportEnum.HighPriority;
    }

    private sealed class CancellationAwareImporter : IImporterViewModel
    {
        public string DisplayName => "cancel-aware";
        public string OutputExtension => ".rigid_model_v2";
        public string[] InputExtensions => [".glb"];
        public bool SupportsCancellation => true;
        public ManualResetEventSlim Started { get; } = new();

        public ImportResult Execute(
            PackFile exportSource,
            string outputPath,
            IPackFileContainer packFileContainer,
            GameTypeEnum gameType,
            IProgress<ImportProgress>? progress,
            CancellationToken cancellationToken)
        {
            Started.Set();
            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
            cancellationToken.ThrowIfCancellationRequested();
            return ImportResult.Success([]);
        }

        public ImportSupportEnum CanImportFile(PackFile file) =>
            ImportSupportEnum.HighPriority;
    }
}
