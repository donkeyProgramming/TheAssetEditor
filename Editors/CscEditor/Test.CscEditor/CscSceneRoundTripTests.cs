using Editors.CscEditor.Data;
using Shared.GameFormats.Esf;

namespace Test.CscEditor
{
    public class CscSceneRoundTripTests
    {
        /// <summary>A ROOT v5 document with no elements, no attach entries and no tail.</summary>
        static byte[] BuildEmptySceneBytes()
        {
            var fields = new List<EsfNode>
            {
                // v5 header: F32, Coord3d, F32, Ascii + F32, Ascii, U32 + Coord3d, Coord3d, F32
                EsfNode.Leaf(EsfNodeKind.F32, 20f),
                EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
                EsfNode.Leaf(EsfNodeKind.F32, 0f),
                EsfNode.Leaf(EsfNodeKind.Ascii, ""),
                EsfNode.Leaf(EsfNodeKind.F32, 0f),
                EsfNode.Leaf(EsfNodeKind.Ascii, ""),
                EsfNode.Leaf(EsfNodeKind.U32, 0u, optimized: true),
                EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
                EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(0, 0, 0)),
                EsfNode.Leaf(EsfNodeKind.F32, 1f),
                // element group count + attach group count
                EsfNode.Leaf(EsfNodeKind.U32, 0u, optimized: true),
                EsfNode.Leaf(EsfNodeKind.U32, 0u, optimized: true),
            };

            var doc = new EsfDocument
            {
                Signature = EsfSignature.Caab,
                Unknown1 = 0,
                CreationDate = 123456,
                Root = EsfNode.NewRecord("ROOT", 5, EsfRecordFlags.IsRecordNode, [fields]),
            };

            using var stream = new MemoryStream();
            EsfWriter.Write(doc, stream);
            return stream.ToArray();
        }

        [Test]
        public void Loads_an_empty_scene()
        {
            var scene = CscScene.Load(BuildEmptySceneBytes());

            Assert.That(scene.Elements, Is.Empty);
            Assert.That(scene.RootVersion, Is.EqualTo(5));
            Assert.That(scene.Duration, Is.EqualTo(20f));
            Assert.That(scene.ManifestKind, Is.EqualTo(CscManifestKind.None));
        }

        [Test]
        public void Inserted_elements_round_trip_with_hierarchy_channels_and_keyframes()
        {
            var scene = CscScene.Load(BuildEmptySceneBytes());

            var model = CscElementFactory.Create(scene, CscElementKind.Model, @"battle_props\thing.rigid_model_v2");
            scene.AddElement(model);

            var vfx = CscElementFactory.Create(scene, CscElementKind.Vfx, "my_effect");
            scene.AddElement(vfx, parent: model);
            vfx.AttachBoneIndex = 7;

            var camera = CscElementFactory.Create(scene, CscElementKind.Camera);
            scene.AddElement(camera);

            var light = CscElementFactory.Create(scene, CscElementKind.SpotLight);
            scene.AddElement(light);

            // Animate: a position curve on the model and an fov curve on the camera.
            model.Position!.Channels[1].AddKeyframe(0, 0);
            model.Position.Channels[1].AddKeyframe(5, 3.5f);
            camera.TypeGroups[0].Channels[0].AddKeyframe(0, 45);
            camera.TypeGroups[0].Channels[0].AddKeyframe(2, 90);

            var bytes = CscSceneWriter.Write(scene);
            var reloaded = CscScene.Load(bytes);

            Assert.That(reloaded.Elements, Has.Count.EqualTo(4));
            Assert.That(reloaded.RootElements, Has.Count.EqualTo(3));

            var reModel = reloaded.FindElement(model.Id)!;
            Assert.That(reModel.Kind, Is.EqualTo(CscElementKind.Model));
            Assert.That(reModel.AssetPath, Is.EqualTo(@"battle_props\thing.rigid_model_v2"));
            Assert.That(reModel.Children, Has.Count.EqualTo(1));
            Assert.That(reModel.Position!.Channels[1].Keyframes, Has.Count.EqualTo(2));
            Assert.That(reModel.Position.Channels[1].Evaluate(5), Is.EqualTo(3.5f).Within(0.001));

            var reVfx = reModel.Children[0];
            Assert.That(reVfx.Kind, Is.EqualTo(CscElementKind.Vfx));
            Assert.That(reVfx.AssetPath, Is.EqualTo("my_effect"));
            Assert.That(reVfx.Parent, Is.SameAs(reModel));
            Assert.That(reVfx.AttachBoneIndex, Is.EqualTo(7));

            var reCamera = reloaded.FindElement(camera.Id)!;
            Assert.That(reCamera.Kind, Is.EqualTo(CscElementKind.Camera));
            Assert.That(reCamera.TypeGroups, Has.Count.EqualTo(13));
            Assert.That(reCamera.TypeGroups[0].Channels[0].Keyframes, Has.Count.EqualTo(2));
            Assert.That(reCamera.TypeGroups[0].Channels[0].Evaluate(2), Is.EqualTo(90).Within(0.001));

            var reLight = reloaded.FindElement(light.Id)!;
            Assert.That(reLight.Kind, Is.EqualTo(CscElementKind.SpotLight));
            Assert.That(reLight.TypeGroups, Has.Count.EqualTo(6));
        }

        [Test]
        public void Reparenting_and_deleting_survive_a_round_trip()
        {
            var scene = CscScene.Load(BuildEmptySceneBytes());
            var a = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(a);
            var b = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(b);
            var c = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(c, parent: b);

            // Move c under a; delete b.
            Assert.That(scene.Reparent(c, a), Is.True);
            scene.RemoveElementSubtree(b);

            // Cycles are rejected.
            Assert.That(scene.Reparent(a, c), Is.False);

            var reloaded = CscScene.Load(CscSceneWriter.Write(scene));
            Assert.That(reloaded.Elements, Has.Count.EqualTo(2));
            Assert.That(reloaded.RootElements, Has.Count.EqualTo(1));
            Assert.That(reloaded.RootElements[0].Id, Is.EqualTo(a.Id));
            Assert.That(reloaded.RootElements[0].Children[0].Id, Is.EqualTo(c.Id));
        }

        // -----------------------------------------------------------------
        // COMPOSITE_SCENE manifest handling
        // -----------------------------------------------------------------

        /// <summary>Locator elements plus a wrapped COMPOSITE_SCENE manifest (unnamed form).</summary>
        internal static byte[] BuildSceneWithManifestBytes(
            int elementCount = 1,
            bool corruptGroupCount = false,
            string entryName = "Test Entry",
            bool entryFlag = false,
            int reservedValue = 0)
        {
            var scratch = CscScene.Load(BuildEmptySceneBytes());
            for (var i = 0; i < elementCount; i++)
                scratch.AddElement(CscElementFactory.Create(scratch, CscElementKind.Locator));

            var fields = new List<EsfNode>(scratch.HeaderFields)
            {
                EsfNode.Leaf(EsfNodeKind.U32, (uint)elementCount, optimized: true),
            };
            foreach (var element in scratch.Elements)
            {
                fields.Add(EsfNode.Leaf(EsfNodeKind.Ascii, element.GroupLabel));
                fields.AddRange(element.Records);
            }

            fields.Add(EsfNode.Leaf(EsfNodeKind.U32, 1u, optimized: true));                       // attach group count
            fields.Add(EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)-1), optimized: true));     // parent -1
            fields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)elementCount, optimized: true));      // child count
            foreach (var element in scratch.Elements)
            {
                fields.Add(EsfNode.Leaf(EsfNodeKind.U32, (uint)element.Id, optimized: true));
                fields.Add(EsfNode.NewRecord("ATTACH_TO_PARAMETERS", 0, EsfRecordFlags.IsRecordNode,
                    [[EsfNode.Leaf(EsfNodeKind.U32, unchecked((uint)-1), optimized: true)]]));
            }

            // Each locator has one unnamed group with 4 channels (position 3, orientation 3,
            // scale 1, weight 1 sub-components), all zero keyframes.
            var manifest = new List<EsfNode>
            {
                EsfNode.Leaf(
                    EsfNodeKind.I32,
                    corruptGroupCount ? elementCount + 1 : elementCount,
                    optimized: true),
            };
            foreach (var element in scratch.Elements)
            {
                manifest.Add(EsfNode.Leaf(EsfNodeKind.U32, 0u, optimized: true));  // marker: unnamed
                manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, 4, optimized: true));   // channel count
                foreach (var subCount in new[] { 3, 3, 1, 1 })
                {
                    manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, subCount, optimized: true));
                    for (var i = 0; i < subCount; i++)
                    {
                        manifest.Add(EsfNode.Leaf(EsfNodeKind.Bool, false, optimized: true));
                        manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true)); // keyframe count
                    }
                }
            }
            manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, 1, optimized: true));         // entry count
            manifest.Add(EsfNode.Leaf(EsfNodeKind.Ascii, entryName));
            manifest.Add(EsfNode.Leaf(EsfNodeKind.Bool, entryFlag, optimized: true));
            manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true));
            manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, reservedValue, optimized: true));
            manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true));
            manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, elementCount, optimized: true));
            foreach (var element in scratch.Elements)
                manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, element.Id, optimized: true));
            manifest.Add(EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true));         // footer unit: no children

            fields.Add(EsfNode.NewRecord("COMPOSITE_SCENE", 0, EsfRecordFlags.IsRecordNode, [manifest]));

            var doc = new EsfDocument
            {
                Signature = EsfSignature.Caab,
                Unknown1 = 0,
                CreationDate = 123456,
                Root = EsfNode.NewRecord("ROOT", 5, EsfRecordFlags.IsRecordNode, [fields]),
            };

            using var stream = new MemoryStream();
            EsfWriter.Write(doc, stream);
            return stream.ToArray();
        }


        [Test]
        public void Manifest_keyframe_counts_are_updated_when_curves_change()
        {
            var scene = CscScene.Load(BuildSceneWithManifestBytes());
            Assert.That(scene.ManifestParsed, Is.True);
            Assert.That(scene.ManifestEntries, Has.Count.EqualTo(1));

            var element = scene.Elements[0];
            Assert.That(element.ManifestGroupFields, Is.Not.Null);

            // Add two keyframes to position X, then save + reload.
            element.Position!.Channels[0].AddKeyframe(0, 0);
            element.Position.Channels[0].AddKeyframe(3, 5);

            var reloaded = CscScene.Load(CscSceneWriter.Write(scene));
            Assert.That(reloaded.ManifestParsed, Is.True);
            Assert.That(reloaded.Elements[0].Position!.Channels[0].Keyframes, Has.Count.EqualTo(2));

            // The reloaded manifest slice restates the new count: parse it back and check.
            var slice = reloaded.Elements[0].ManifestGroupFields!;
            var groups = Shared.GameFormats.Csc.CompositeSceneDetector.DetectGroups(slice);
            Assert.That(groups, Has.Count.EqualTo(1));
            Assert.That(groups[0].Channels[0].SubComponents[0].KeyframeCount, Is.EqualTo(2));

            // The named entry survived too.
            Assert.That(reloaded.ManifestEntries[0].Fields[0].Value, Is.EqualTo("Test Entry"));
        }

        [Test]
        public void New_elements_get_a_manifest_group_when_the_scene_has_a_manifest()
        {
            var scene = CscScene.Load(BuildSceneWithManifestBytes());
            var vfx = CscElementFactory.Create(scene, CscElementKind.Vfx, "sparkles");
            scene.AddElement(vfx, parent: scene.Elements[0]);

            var reloaded = CscScene.Load(CscSceneWriter.Write(scene));
            Assert.That(reloaded.ManifestParsed, Is.True);
            Assert.That(reloaded.Elements, Has.Count.EqualTo(2));

            var reVfx = reloaded.FindElement(vfx.Id)!;
            Assert.That(reVfx.ManifestGroupFields, Is.Not.Null,
                "the new element should have been given a manifest group on save");

            // Entry section: the original entry plus a generated one for the new element.
            Assert.That(reloaded.ManifestEntries, Has.Count.EqualTo(2));

            // Footer adjacency: the parent's entry should now list the child's entry index.
            // (Verified indirectly: the reloaded manifest parsed cleanly, which requires the
            // footer to be exactly entryCount units.)
        }

        [Test]
        public void Writer_rejects_duplicate_ids_before_serializing()
        {
            var scene = CscScene.Load(BuildEmptySceneBytes());
            var first = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(first);
            var second = CscElementFactory.Create(scene, CscElementKind.Locator);
            second.Id = first.Id;
            scene.AddElement(second);

            Assert.Throws<InvalidDataException>(() => CscSceneWriter.Write(scene));
        }

        [Test]
        public void Writer_rejects_an_attach_cycle_before_serializing()
        {
            var scene = CscScene.Load(BuildEmptySceneBytes());
            var a = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(a);
            var b = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(b, a);

            // Corrupt the tree behind Reparent's own cycle check.
            a.Parent = b;
            b.Children.Add(a);
            scene.RootElements.Remove(a);

            Assert.Throws<InvalidDataException>(() => CscSceneWriter.Write(scene));
        }

        [Test]
        public void Writer_rejects_an_element_missing_from_the_attach_tree()
        {
            var scene = CscScene.Load(BuildEmptySceneBytes());
            var orphan = CscElementFactory.Create(scene, CscElementKind.Locator);
            scene.AddElement(orphan);
            scene.RootElements.Remove(orphan);

            Assert.Throws<InvalidDataException>(() => CscSceneWriter.Write(scene));
        }

        [TestCase("add")]
        [TestCase("delete")]
        [TestCase("reparent")]
        public void Structural_edits_are_rejected_when_the_manifest_cannot_be_parsed(string operation)
        {
            var scene = CscScene.Load(BuildSceneWithManifestBytes(elementCount: 2, corruptGroupCount: true));
            Assert.That(scene.ManifestParsed, Is.False);

            TestDelegate edit = operation switch
            {
                "add" => () => scene.AddElement(CscElementFactory.Create(scene, CscElementKind.Locator)),
                "delete" => () => scene.RemoveElementSubtree(scene.Elements[0]),
                "reparent" => () => scene.Reparent(scene.Elements[1], scene.Elements[0]),
                _ => throw new ArgumentOutOfRangeException(nameof(operation)),
            };

            Assert.Throws<InvalidOperationException>(edit);
            Assert.That(scene.Elements, Has.Count.EqualTo(2));
            Assert.That(scene.RootElements, Has.Count.EqualTo(2));
        }
    }
}
