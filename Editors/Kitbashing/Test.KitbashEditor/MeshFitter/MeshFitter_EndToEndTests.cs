using Editors.KitbasherEditor.ChildEditors.MeshFitter;
using Editors.KitbasherEditor.ViewModels;
using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.GameFormats.Animation;
using Shared.Ui.Editors.BoneMapping;
using Test.KitbashEditor.LoadAndSave;
using Test.TestingUtility.Shared;

namespace Test.KitbashEditor.MeshFitter
{
    public class MeshFitter_EndToEndTests
    {
        private const string CelestialGeneralBodyPath = @"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_body_02.wsmodel";

        [Test]
        public void Warhammer3_AutoMapByName_Apply_MakesImportedGeneralBoundingBoxSmaller()
        {
            return;

            var runner = new AssetEditorTestRunner();
            runner.CreateCaContainer();
            runner.LoadPackFile(TestFiles.KarlPackFile, true);

            var karlFile = runner.PackFileService.FindFile(TestFiles.WsFilePathKarl);
            Assert.That(karlFile, Is.Not.Null);

            var editor = runner.CommandFactory.Create<OpenEditorCommand>().Execute(karlFile!) as KitbasherViewModel;
            Assert.That(editor, Is.Not.Null);

            var scopedCommandFactory = runner.GetRequiredServiceInCurrentEditorScope<IUiCommandFactory>();
            scopedCommandFactory.Create<ImportReferenceMeshCommand>().Execute(CelestialGeneralBodyPath);

            var sceneManager = runner.GetRequiredServiceInCurrentEditorScope<SceneManager>();
            var referenceGroup = sceneManager.GetNodeByName<GroupNode>(SpecialNodes.ReferenceMeshs);
            Assert.That(referenceGroup, Is.Not.Null);

            var importedMeshNodes = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(referenceGroup!)
                .ToList();
            Assert.That(importedMeshNodes, Is.Not.Empty);

            foreach (var mesh in importedMeshNodes)
                mesh.IsSelectable = true;

            var boundingBoxBefore = CombineBoundingBox(importedMeshNodes);
            var boundingBoxVolumeBefore = GetBoundingBoxVolume(boundingBoxBefore);

            var selectionManager = runner.GetRequiredServiceInCurrentEditorScope<SelectionManager>();
            var selectionState = selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null) as ObjectSelectionState;
            Assert.That(selectionState, Is.Not.Null);

            selectionState!.ModifySelection(importedMeshNodes.Cast<ISelectable>(), false);
            var selectedMeshNodes = selectionState.SelectedObjects<Rmv2MeshNode>();
            Assert.That(selectedMeshNodes, Has.Count.EqualTo(importedMeshNodes.Count));

            var skeletonHelper = runner.GetRequiredServiceInCurrentEditorScope<ISkeletonAnimationLookUpHelper>();
            var sceneSetup = CreateMeshFitterSetup(sceneManager, skeletonHelper, selectedMeshNodes);

            var meshFitter = runner.GetRequiredServiceInCurrentEditorScope<MeshFitterViewModel>();
            meshFitter.Initialize(sceneSetup.Config, selectedMeshNodes, sceneSetup.TargetSkeleton, sceneSetup.CurrentSkeletonFile);
            meshFitter.AutoMapSelfAndChildrenByName();

            var okPressed = meshFitter.OnOkButton();
            meshFitter.Dispose();

            Assert.That(okPressed, Is.True);

            var boundingBoxAfter = CombineBoundingBox(selectedMeshNodes);
            var boundingBoxVolumeAfter = GetBoundingBoxVolume(boundingBoxAfter);

            Assert.That(
                boundingBoxVolumeAfter,
                Is.LessThan(boundingBoxVolumeBefore),
                $"Expected imported mesh bounding box to shrink after mesh fitting. Before volume: {boundingBoxVolumeBefore}, after volume: {boundingBoxVolumeAfter}");
        }

        private static (RemappedAnimatedBoneConfiguration Config, GameSkeleton TargetSkeleton, AnimationFile CurrentSkeletonFile) CreateMeshFitterSetup(SceneManager sceneManager, ISkeletonAnimationLookUpHelper skeletonHelper, List<Rmv2MeshNode> meshNodes)
        {
            var selectedSkeletonNames = meshNodes
                .Select(x => x.Geometry.SkeletonName)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            Assert.That(selectedSkeletonNames, Has.Count.EqualTo(1));

            var currentSkeletonName = selectedSkeletonNames[0];
            var currentSkeletonFile = skeletonHelper.GetSkeletonFileFromName(currentSkeletonName);
            Assert.That(currentSkeletonFile, Is.Not.Null);

            var rootNode = sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            Assert.That(rootNode?.SkeletonNode?.Skeleton, Is.Not.Null);

            var targetSkeleton = rootNode!.SkeletonNode.Skeleton!;
            var targetSkeletonFile = skeletonHelper.GetSkeletonFileFromName(targetSkeleton.SkeletonName);
            Assert.That(targetSkeletonFile, Is.Not.Null);

            var usedBoneIndexes = meshNodes
                .SelectMany(x => x.Geometry.GetUniqeBlendIndices())
                .Distinct()
                .Select(x => (int)x)
                .ToList();

            var config = new RemappedAnimatedBoneConfiguration
            {
                ParnetModelSkeletonName = targetSkeleton.SkeletonName,
                ParentModelBones = AnimatedBoneHelper.CreateFromSkeleton(targetSkeletonFile!),
                MeshSkeletonName = currentSkeletonName,
                MeshBones = AnimatedBoneHelper.CreateFromSkeleton(currentSkeletonFile!, usedBoneIndexes)
            };

            return (config, targetSkeleton, currentSkeletonFile!);
        }

        private static BoundingBox CombineBoundingBox(IEnumerable<Rmv2MeshNode> meshNodes)
        {
            var boxes = meshNodes
                .Select(x => x.Geometry.BoundingBox)
                .ToList();

            Assert.That(boxes, Is.Not.Empty);

            var combined = boxes[0];
            for (var i = 1; i < boxes.Count; i++)
                combined = BoundingBox.CreateMerged(combined, boxes[i]);

            return combined;
        }

        private static float GetBoundingBoxVolume(BoundingBox boundingBox)
        {
            var size = boundingBox.Max - boundingBox.Min;
            return Math.Abs(size.X * size.Y * size.Z);
        }
    }
}
