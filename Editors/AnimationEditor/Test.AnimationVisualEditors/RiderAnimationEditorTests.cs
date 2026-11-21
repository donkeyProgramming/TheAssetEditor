using AnimationEditor.MountAnimationCreator;
using AnimationEditor.MountAnimationCreator.Services;
using Editors.AnimationVisualEditors.MountAnimationCreator.Services;
using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Moq;
using Shared.Core.Commands;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;
using Test.TestingUtility.Shared;
using static Test.AnimationVisualEditors.VanillaRiders;

namespace Test.AnimationVisualEditors
{
    public class RiderAnimationEditorTests
    {
        record Tool(AssetEditorTestRunner Runner, IPackFileContainer Output, MountAnimationCreatorViewModel Editor, SceneObject Rider, SceneObject Mount);

        static Tool Open(string riderSet)
        {
            var runner = VanillaRiders.Load();
            var output = runner.CreateOutputPack();
            var editor = (MountAnimationCreatorViewModel)runner.CommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.MountTool_Editor);
            var sceneObjectEditor = runner.GetRequiredServiceInCurrentEditorScope<SceneObjectEditor>();
            var rider = editor.GetSceneObjectFromId(MountToolIds.Rider)!.Data;
            var mount = editor.GetSceneObjectFromId(MountToolIds.Mount)!.Data;
            sceneObjectEditor.SetMesh(rider, runner.PackFileService.FindFile(RiderMesh)!);
            sceneObjectEditor.SetMesh(mount, runner.PackFileService.FindFile(MountMesh)!);

            var sets = editor.Sets;
            sets.Mount.FragmentList.SelectedItem = sets.Mount.FragmentList.PossibleValues.First(x => x.Name == MountSet);
            sets.Rider.FragmentList.SelectedItem = sets.Rider.FragmentList.PossibleValues.First(x => x.Name == riderSet);
            return new Tool(runner, output, editor, rider, mount);
        }

        static void PickMountSlot(Tool tool, string slot)
        {
            var sets = tool.Editor.Sets;
            sets.Mount.FragmentSlotList.SelectedItem = sets.Mount.FragmentSlotList.PossibleValues.First(x => x.SlotName == slot);
            Assert.That(sets.Rider.FragmentSlotList.SelectedItem?.SlotName, Is.EqualTo("RIDER_" + slot), "the matching rider slot is picked with it");
        }

        static RiderAttachmentConverter Converter(Tool tool)
        {
            var tech = Skeleton(tool.Runner.PackFileService, TechSkeleton);
            return new RiderAttachmentConverter(tool.Rider.Skeleton, tool.Mount.Skeleton, tool.Mount.AnimationClip, tech, tech.GetBoneIndexByName(AttachBone));
        }

        static AnimationBinWh3 SavedSet(Tool tool, string name)
        {
            var file = tool.Output.SearchFiles(null, null).Single(x => string.Equals(x.Path, RiderSetBatchService.AnimPackPath(name), StringComparison.OrdinalIgnoreCase)).File;
            var animPack = AnimationPackSerializer.Load(file, tool.Runner.PackFileService);
            return (AnimationBinWh3)animPack.GetGenericAnimationSets().Single();
        }

        static List<string> SavedFiles(Tool tool) => tool.Output.SearchFiles(null, null).Select(x => x.Path).ToList();

        // Picks the mount vertex nearest the saddle, the way the viewport's vertex selection does
        static int PickSaddleVertex(Tool tool)
        {
            var mesh = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(tool.Mount.ModelNode).First();
            var saddle = new Vector3(0, 1.64f, 0.6f);
            var vertex = Enumerable.Range(0, mesh.Geometry.VertexCount()).OrderBy(i => (mesh.Geometry.GetVertexById(i) - saddle).LengthSquared()).First();
            var selection = tool.Runner.GetRequiredServiceInCurrentEditorScope<SelectionManager>();
            var state = (VertexSelectionState)selection.CreateSelectionSate(GeometrySelectionMode.Vertex, mesh);
            state.ModifySelection([vertex], false);
            tool.Editor.SetMountVertex();
            return vertex;
        }

        [Test]
        public void AttachConversion_ConvertsTheRiderAndFollowsTheSlot()
        {
            var tool = Open(AttachedSet);
            var converter = tool.Editor.AttachedRiders;
            tool.Editor.SelectedModeTab.Value = MountAnimationCreatorViewModel.AttachConversionTab;

            Assert.That(converter.CanConvert.Value, Is.False);
            Assert.That(converter.ConvertStatus.Value, Is.Not.Empty, "says what's missing");

            tool.Editor.AttachPoint.SetAttachSkeleton(tool.Runner.PackFileService.FindFile(TechSkeleton)!);
            Assert.That(tool.Editor.AttachPoint.AttachBones, Is.EqualTo(new[] { AttachBone }));
            Assert.That(tool.Editor.AttachPoint.SelectedAttachBone.Value, Is.EqualTo(AttachBone));
            PickMountSlot(tool, "RUN_1");
            Assert.That(converter.CanConvert.Value, Is.True);
            Assert.That(converter.ConvertStatus.Value, Is.Empty);

            // Kislev is attached: to synchronized, previewed in place
            var preview = tool.Editor.Preview;
            converter.ConvertRiderToSynchronized();
            Assert.That(preview.AnimationClip.DynamicFrames[7].Position, Is.EqualTo(Converter(tool).ToSynchronized(tool.Rider.AnimationClip).DynamicFrames[7].Position));
            preview.Player.CurrentFrame = 7;
            Assert.That(preview.Offset, Is.EqualTo(Matrix.Identity));

            // To attached: previewed on the attach bone, frame by frame, where the game puts it
            converter.ConvertRiderToAttached();
            foreach (var frame in new[] { 7, 3 })
            {
                preview.Player.CurrentFrame = frame;
                Assert.That(preview.Offset, Is.EqualTo(Converter(tool).AttachPointWorld(frame, preview.AnimationClip.DynamicFrames.Count)));
            }

            // Another slot is converted the same way
            PickMountSlot(tool, "WALK_1");
            Assert.That(preview.AnimationClip.DynamicFrames.Count, Is.EqualTo(tool.Rider.AnimationClip.DynamicFrames.Count));
            Assert.That(preview.AnimationClip.DynamicFrames[5].Rotation, Is.EqualTo(Converter(tool).ToAttached(tool.Rider.AnimationClip).DynamicFrames[5].Rotation));

            converter.SaveConvertedAnimation();
            var saved = SavedFiles(tool).Single();
            Assert.That(saved, Does.EndWith("new_" + Path.GetFileName(tool.Rider.AnimationName.Value)));

            // The other tab takes the preview back
            tool.Editor.SelectedModeTab.Value = MountAnimationCreatorViewModel.SynchronizedMountChangeTab;
            Assert.That(converter.CanSave.Value, Is.False);
            Assert.That(preview.Offset, Is.EqualTo(Matrix.Identity));
        }

        // Converts the picked rider set into "test_riders" and returns the log
        static ErrorList ConvertSet(Tool tool, bool toAttached)
        {
            var converter = tool.Editor.AttachedRiders;
            tool.Runner.Dialogs.Setup(x => x.ShowTextInputDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(new TextInputDialogResult(true, "test_riders"));
            ErrorList? log = null;
            tool.Runner.Dialogs.Setup(x => x.ShowErrorViewDialog(It.IsAny<string>(), It.IsAny<ErrorList>(), It.IsAny<bool>())).Callback<string, ErrorList, bool>((_, l, _) => log = l);

            tool.Editor.AttachPoint.SetAttachSkeleton(tool.Runner.PackFileService.FindFile(TechSkeleton)!);
            Assert.That(converter.CanConvertSet.Value, Is.True);
            if (toAttached) converter.ConvertSetToAttached(); else converter.ConvertSetToSynchronized();
            foreach (var line in log!.Errors)
                TestContext.Out.WriteLine($"{line.ErrorType} {line.ItemName}: {line.Description}");
            Assert.That(log.Errors.Where(x => x.IsError), Is.Empty);
            return log;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AttachConversion_ConvertsTheWholeSet(bool toAttached)
        {
            // Empire is synchronized and Kislev attached; each converts to the other
            var tool = Open(toAttached ? SynchronizedSet : AttachedSet);
            Assert.That(tool.Editor.AttachedRiders.CanConvertSet.Value, Is.False, "no tech skeleton yet");
            ConvertSet(tool, toAttached);

            var pfs = tool.Runner.PackFileService;
            var original = (AnimationBinWh3)tool.Editor.Sets.Rider.FragmentList.SelectedItem!;
            var converted = SavedSet(tool, "test_riders");
            Assert.That(converted.Name, Is.EqualTo("test_riders"));
            Assert.That(converted.MountBin, Is.EqualTo(MountSet));
            Assert.That(converted.SkeletonName, Is.EqualTo(original.SkeletonName));
            Assert.That(converted.AnimationTableEntries.Select(x => (x.AnimationId, x.AnimationRefs.Count)), Is.EqualTo(original.AnimationTableEntries.Select(x => (x.AnimationId, x.AnimationRefs.Count))));

            var slots = ((IAnimationBinGenericFormat)converted).Entries;
            var originalSlots = ((IAnimationBinGenericFormat)original).Entries;
            var rider = Skeleton(pfs, @"animations\skeletons\humanoid01.anim");
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i].SlotName;
                var changed = slots[i].AnimationFile != originalSlots[i].AnimationFile;
                var shouldChange = slot is "RIDER_RUN_1" or "RIDER_STAND" or "RIDER_ATTACK_1";
                var shouldStay = !slot.StartsWith("RIDER_") || slot.StartsWith("RIDER_DEATH_") || slot == "RIDER_MOVING_ATTACK_1";
                if (shouldChange)
                {
                    Assert.That(changed, Is.True, slot);
                    Assert.That(slots[i].AnimationFile, Does.Contain("new_"), slot);
                    Assert.That(RiderAttachmentConverter.IsAttached(Clip(pfs, slots[i].AnimationFile, rider)), Is.EqualTo(toAttached), slot);
                }
                if (shouldStay)
                    Assert.That(changed, Is.False, slot);
                Assert.That(slots[i].MetaFile, Is.EqualTo(originalSlots[i].MetaFile), slot);
            }

            // The new set shows up in the set list, and the picked set stays picked
            Assert.That(tool.Editor.Sets.Rider.FragmentList.PossibleValues.Select(x => x.Name), Does.Contain("test_riders"));
            Assert.That(tool.Editor.Sets.Rider.FragmentList.SelectedItem?.Name, Is.EqualTo(original.Name));
        }

        // A rider animation shared by slots whose mount animations differ is converted once per mount animation.
        // With unique names off, those conversions must still go to separate files.
        [TestCase(true)]
        [TestCase(false)]
        public void AttachConversion_ConvertsTheWholeSet_WithoutUniqueNames_GivesEachMountAnimationItsOwnFile(bool toAttached)
        {
            var tool = Open(toAttached ? SynchronizedSet : AttachedSet);
            tool.Editor.Output.EnsureUniqueFileName.Value = false;
            var log = ConvertSet(tool, toAttached);

            var saved = log.Errors.Where(x => x.Description.StartsWith("Saved ")).Select(x => x.Description[6..].Split(" (")[0]).ToList();
            Assert.That(saved, Is.Unique);
            Assert.That(saved, Has.Some.Contains("new_1_"), "a rider animation was converted against two mount animations");
            Assert.That(SavedFiles(tool).Count(x => x.EndsWith(".anim")), Is.EqualTo(saved.Count));
        }

        [Test]
        public void ReloadingTheSets_KeepsBothPickedSlots()
        {
            var tool = Open(SynchronizedSet);
            PickMountSlot(tool, "RUN_1");
            var sets = tool.Editor.Sets;
            sets.Rider.FragmentSlotList.SelectedItem = sets.Rider.FragmentSlotList.PossibleValues.First(x => x.SlotName == "RIDER_WALK_1");
            var riderAnimation = tool.Rider.AnimationName.Value;

            // Saving an animpack reloads the set lists
            var riderBin = (AnimationBinWh3)sets.Rider.FragmentList.SelectedItem!;
            var animPack = new AnimationPackFileDatabase("test_reload");
            animPack.AddFile(new AnimationBinWh3("animations/database/battle/bin/test_reload.bin")
            {
                TableVersion = riderBin.TableVersion,
                TableSubVersion = riderBin.TableSubVersion,
                Name = "test_reload",
                MountBin = MountSet,
                Unknown = riderBin.Unknown,
                SkeletonName = riderBin.SkeletonName,
                LocomotionGraph = riderBin.LocomotionGraph,
                UnknownValue1 = riderBin.UnknownValue1,
            });
            var fileSaveService = (IFileSaveService)tool.Runner.ServiceProvider.GetService(typeof(IFileSaveService))!;
            fileSaveService.Save(RiderSetBatchService.AnimPackPath("test_reload"), AnimationPackSerializer.ConvertToBytes(animPack), false);

            Assert.That(sets.Rider.FragmentList.PossibleValues.Select(x => x.Name), Does.Contain("test_reload"), "the set lists were reloaded");
            Assert.That(sets.Mount.FragmentSlotList.SelectedItem?.SlotName, Is.EqualTo("RUN_1"));
            Assert.That(sets.Rider.FragmentSlotList.SelectedItem?.SlotName, Is.EqualTo("RIDER_WALK_1"));
            Assert.That(tool.Rider.AnimationName.Value, Is.EqualTo(riderAnimation));
        }

        [Test]
        public void AttachedMountChange_PreviewsTheRiderOnTheAttachBone()
        {
            var tool = Open(AttachedSet);
            var tab = tool.Editor.AttachedMountChange;
            tool.Editor.SelectedModeTab.Value = MountAnimationCreatorViewModel.AttachedMountChangeTab;
            tab.Settings.FitAnimation = false;
            PickMountSlot(tool, "RUN_1");

            var preview = tool.Editor.Preview;
            Assert.That(preview.AnimationClip, Is.Not.Null);
            Assert.That(RiderAttachmentConverter.IsAttached(preview.AnimationClip), Is.True);
            Assert.That(tab.PreviewStatus.Value, Is.Not.Empty, "asks for the tech skeleton");
            Assert.That(preview.Offset, Is.EqualTo(Matrix.Identity));

            tool.Editor.AttachPoint.SetAttachSkeleton(tool.Runner.PackFileService.FindFile(TechSkeleton)!);
            Assert.That(tab.PreviewStatus.Value, Is.Empty);
            foreach (var frame in new[] { 7, 3 })
            {
                preview.Player.CurrentFrame = frame;
                Assert.That(preview.Offset, Is.EqualTo(Converter(tool).AttachPointWorld(frame, preview.AnimationClip.DynamicFrames.Count)));
            }

            // An offset moves the rider away from the attach bone; animroot stays on it
            tab.Settings.Translation.Set(0, 0.1f, 0);
            Assert.That(RiderAttachmentConverter.IsAttached(preview.AnimationClip), Is.True);
            var root = tool.Rider.Skeleton.GetBoneIndexByName("root");
            for (var f = 0; f < preview.AnimationClip.DynamicFrames.Count; f++)
                Assert.That(World(preview.Skeleton, preview.AnimationClip, f, root) - World(tool.Rider.Skeleton, tool.Rider.AnimationClip, f, root), Is.EqualTo(new Vector3(0, 0.1f, 0)).Using<Vector3>((a, b) => (a - b).Length() < 1e-4f), $"frame {f}");

            // The other tabs take the preview back
            tool.Editor.SelectedModeTab.Value = MountAnimationCreatorViewModel.SynchronizedMountChangeTab;
            Assert.That(preview.Offset, Is.EqualTo(Matrix.Identity));
        }

        [Test]
        public void AttachedMountChange_MovesTheWholeSet()
        {
            var tool = Open(AttachedSet);
            tool.Runner.Dialogs.Setup(x => x.ShowTextInputDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(new TextInputDialogResult(true, "test_riders"));
            ErrorList? log = null;
            tool.Runner.Dialogs.Setup(x => x.ShowErrorViewDialog(It.IsAny<string>(), It.IsAny<ErrorList>(), It.IsAny<bool>())).Callback<string, ErrorList, bool>((_, l, _) => log = l);

            // Stretched to the mount, and moved up so every attached animation changes
            tool.Editor.AttachedMountChange.Settings.Translation.Set(0, 0.05f, 0);
            tool.Editor.AttachedMountChange.RefitSet();
            foreach (var line in log!.Errors)
                TestContext.Out.WriteLine($"{line.ErrorType} {line.ItemName}: {line.Description}");
            Assert.That(log.Errors.Where(x => x.IsError), Is.Empty);

            var pfs = tool.Runner.PackFileService;
            var original = (IAnimationBinGenericFormat)tool.Editor.Sets.RiderSet!;
            var made = (IAnimationBinGenericFormat)SavedSet(tool, "test_riders");
            var mountSet = Set(pfs, MountSet);
            var rider = Skeleton(pfs, @"animations\skeletons\humanoid01.anim");
            Assert.That(made.MountBin, Is.EqualTo(MountSet));
            Assert.That(made.Entries.Select(x => x.SlotName), Is.EqualTo(original.Entries.Select(x => x.SlotName)));

            var changedCount = 0;
            for (var i = 0; i < made.Entries.Count; i++)
            {
                var slot = made.Entries[i].SlotName;
                var changed = made.Entries[i].AnimationFile != original.Entries[i].AnimationFile;
                if (pfs.FindFile(original.Entries[i].AnimationFile) == null)
                {
                    Assert.That(changed, Is.False, $"{slot} names a missing animation");
                    continue;
                }
                var originalClip = Clip(pfs, original.Entries[i].AnimationFile, rider);
                var hasMountSlot = slot.StartsWith("RIDER_") && mountSet.Entries.Any(x => x.SlotName == slot[6..]);
                Assert.That(changed, Is.EqualTo(hasMountSlot && RiderAttachmentConverter.IsAttached(originalClip)), slot);
                Assert.That(made.Entries[i].MetaFile, Is.EqualTo(original.Entries[i].MetaFile), slot);
                if (!changed)
                    continue;

                changedCount++;
                var newClip = Clip(pfs, made.Entries[i].AnimationFile, rider);
                Assert.That(RiderAttachmentConverter.IsAttached(newClip), Is.True, slot);
                var mountVariants = mountSet.Entries.Where(x => x.SlotName == slot[6..]).ToList();
                if (mountVariants.Count == 1)
                    Assert.That(newClip.DynamicFrames.Count, Is.EqualTo(Clip(pfs, mountVariants[0].AnimationFile, Skeleton(pfs, @"animations\skeletons\horse01.anim")).DynamicFrames.Count), $"{slot} stretched to the mount");
            }
            Assert.That(changedCount, Is.GreaterThan(10));
            Assert.That(made.Entries.Where(x => RiderAttachmentConverter.StaysSynchronized(x.SlotName)).Select(x => x.AnimationFile),
                Is.EqualTo(original.Entries.Where(x => RiderAttachmentConverter.StaysSynchronized(x.SlotName)).Select(x => x.AnimationFile)), "deaths and kills copied unchanged");
        }

        [Test]
        public void SynchronizedMountChange_GluesTheRiderToTheNewMountsVertex()
        {
            var tool = Open(SynchronizedSet);
            PickMountSlot(tool, "RUN_1");
            Assert.That(tool.Editor.CanPreview.Value, Is.False, "no vertex yet");
            Assert.That(tool.Editor.PreviewStatus.Value, Is.Not.Empty, "says what's missing");

            var vertex = PickSaddleVertex(tool);
            Assert.That(tool.Editor.CanPreview.Value, Is.True);
            Assert.That(tool.Editor.PreviewStatus.Value, Is.Empty);

            // The preview is made as soon as everything is there; the rider's root follows the vertex on every frame
            var preview = tool.Editor.Preview;
            Assert.That(preview.AnimationClip, Is.Not.Null);
            Assert.That(preview.AnimationClip.DynamicFrames.Count, Is.EqualTo(tool.Mount.AnimationClip.DynamicFrames.Count));
            var mesh = SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(tool.Mount.ModelNode).First();
            var vertexWorld = new MeshAnimationHelper(mesh, Matrix.Identity);
            var root = tool.Rider.Skeleton.GetBoneIndexByName("root");
            for (var f = 0; f < preview.AnimationClip.DynamicFrames.Count; f++)
            {
                var v = vertexWorld.GetVertexTransformWorld(AnimationSampler.Sample(f, 0, tool.Mount.Skeleton, tool.Mount.AnimationClip), vertex).Translation;
                Assert.That((World(preview.Skeleton, preview.AnimationClip, f, root) - v).Length(), Is.LessThan(0.01f), $"frame {f}");
            }

            tool.Editor.SaveCurrentAnimationAction();
            Assert.That(SavedFiles(tool).Single(), Does.EndWith("new_" + Path.GetFileName(tool.Rider.AnimationName.Value)));

            // The other tab clears it
            tool.Editor.SelectedModeTab.Value = MountAnimationCreatorViewModel.AttachConversionTab;
            Assert.That(preview.AnimationClip, Is.Null);
        }

        [Test]
        public void SynchronizedMountChange_RefitsTheWholeRiderSet()
        {
            var tool = Open(SynchronizedSet);
            Assert.That(tool.Editor.CanBatchProcess.Value, Is.False);
            Assert.That(tool.Editor.BatchStatus.Value, Is.Not.Empty, "says what's missing");
            PickMountSlot(tool, "STAND");
            PickSaddleVertex(tool);
            Assert.That(tool.Editor.CanBatchProcess.Value, Is.True);

            tool.Runner.Dialogs.Setup(x => x.ShowTextInputDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(new TextInputDialogResult(true, "test_riders"));
            tool.Editor.BatchProcessUsingFragmentsAction();
            tool.Runner.Dialogs.Verify(x => x.ShowErrorViewDialog(It.IsAny<string>(), It.IsAny<ErrorList>(), false), Times.Once);

            var original = (IAnimationBinGenericFormat)tool.Editor.Sets.Rider.FragmentList.SelectedItem!;
            var made = (IAnimationBinGenericFormat)SavedSet(tool, "test_riders");
            Assert.That(made.MountBin, Is.EqualTo(MountSet));
            Assert.That(made.Entries.Select(x => x.SlotName), Is.EqualTo(original.Entries.Select(x => x.SlotName)), "every slot and variant of the rider set");
            string File(IAnimationBinGenericFormat set, string slot) => set.Entries.First(x => x.SlotName == slot).AnimationFile;
            Assert.That(File(made, "RIDER_RUN_1"), Does.Contain("new_"));
            Assert.That(File(made, "RIDER_MOVING_ATTACK_1"), Is.EqualTo(File(original, "RIDER_MOVING_ATTACK_1")), "the game blends it over the riding motion");

            // Combat ready plays with the mount's own COMBAT_READY like any synchronized slot (in this set it's the
            // RIDER_STAND animation), so it's refitted too
            Assert.That(File(made, "RIDER_COMBAT_READY"), Does.Contain("new_"));
        }

        [Test]
        public void Reset_ReplacesTheToolWithAnEmptyOne()
        {
            var tool = Open(SynchronizedSet);
            var editors = tool.Runner.ServiceProvider.GetService(typeof(IEditorManager)) as IEditorManager;
            tool.Runner.Dialogs.Setup(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>())).Returns(ShowMessageBoxResult.OK);
            tool.Editor.ResetTool();
            tool.Runner.Dialogs.Verify(x => x.ShowYesNoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            var open = editors!.GetAllEditors();
            Assert.That(open, Has.Count.EqualTo(1));
            var fresh = (MountAnimationCreatorViewModel)open[0];
            Assert.That(fresh, Is.Not.SameAs(tool.Editor));
            Assert.That(fresh.GetSceneObjectFromId(MountToolIds.Rider)!.Data.MeshName.Value, Is.Empty);
            Assert.That(fresh.Sets.Rider.FragmentList.SelectedItem, Is.Null);
        }
    }
}
