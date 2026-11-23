using Editors.CscEditor.Data;
using Editors.CscEditor.Services;

namespace Test.CscEditor
{
    public class CscEditHistoryTests
    {
        /// <summary>A one-byte "document" stands in for the scene so the tests read clearly.</summary>
        sealed class Harness
        {
            public byte Value;
            public CscSelectionSnapshot Selection = CscSelectionSnapshot.None;
            public CscSelectionSnapshot? AppliedSelection;
            public int ApplyCount;
            public CscEditHistory History { get; }

            public Harness()
            {
                History = new CscEditHistory(
                    () => [Value],
                    () => Selection,
                    (bytes, selection) =>
                    {
                        Value = bytes[0];
                        AppliedSelection = selection;
                        ApplyCount++;
                    });
                History.InitializeLoadedDocument([Value]);
            }

            public void Edit(byte value)
            {
                Value = value;
                History.RecordChange();
            }
        }

        [Test]
        public void A_fresh_document_has_no_history_and_no_unsaved_changes()
        {
            var h = new Harness();
            Assert.Multiple(() =>
            {
                Assert.That(h.History.CanUndo, Is.False);
                Assert.That(h.History.CanRedo, Is.False);
                Assert.That(h.History.HasUnsavedChanges, Is.False);
            });
        }

        [Test]
        public void Undo_and_redo_walk_the_recorded_steps()
        {
            var h = new Harness();
            h.Edit(1);
            h.Edit(2);

            Assert.That(h.History.Undo(), Is.True);
            Assert.That(h.Value, Is.EqualTo(1));
            Assert.That(h.History.Undo(), Is.True);
            Assert.That(h.Value, Is.EqualTo(0));
            Assert.That(h.History.Undo(), Is.False);

            Assert.That(h.History.Redo(), Is.True);
            Assert.That(h.Value, Is.EqualTo(1));
            Assert.That(h.History.Redo(), Is.True);
            Assert.That(h.Value, Is.EqualTo(2));
            Assert.That(h.History.Redo(), Is.False);
        }

        [Test]
        public void A_change_that_leaves_the_bytes_the_same_records_nothing()
        {
            var h = new Harness();
            h.Edit(0);
            Assert.That(h.History.CanUndo, Is.False);
            Assert.That(h.History.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void A_new_change_after_undo_drops_the_redo_steps()
        {
            var h = new Harness();
            h.Edit(1);
            h.Edit(2);
            h.History.Undo();

            h.Edit(5);

            Assert.That(h.History.CanRedo, Is.False);
            h.History.Undo();
            Assert.That(h.Value, Is.EqualTo(1));
        }

        [Test]
        public void Unsaved_changes_follow_the_saved_state_through_undo_and_redo()
        {
            var h = new Harness();
            h.Edit(1);
            Assert.That(h.History.HasUnsavedChanges, Is.True);

            h.History.MarkSaved();
            Assert.That(h.History.HasUnsavedChanges, Is.False);

            h.History.Undo();
            Assert.That(h.History.HasUnsavedChanges, Is.True);

            h.History.Redo();
            Assert.That(h.History.HasUnsavedChanges, Is.False);

            // Undo back to the loaded file after saving a later state is still a change.
            h.Edit(2);
            h.History.MarkSaved();
            h.History.Undo();
            h.History.Undo();
            Assert.That(h.Value, Is.EqualTo(0));
            Assert.That(h.History.HasUnsavedChanges, Is.True);
        }

        [Test]
        public void A_gesture_becomes_one_step_with_the_selection_from_before_it()
        {
            var h = new Harness { Selection = CscSelectionSnapshot.Element(7) };
            h.History.BeginGesture();
            h.Edit(1);
            h.Edit(2);
            h.Selection = CscSelectionSnapshot.Element(9);
            h.Edit(3);
            Assert.That(h.History.CanUndo, Is.False, "no undo mid-gesture");
            Assert.That(h.History.EndGesture(), Is.True);

            h.History.Undo();
            Assert.That(h.Value, Is.EqualTo(0));
            Assert.That(h.AppliedSelection, Is.EqualTo(CscSelectionSnapshot.Element(7)));
            Assert.That(h.History.CanUndo, Is.False);

            h.History.Redo();
            Assert.That(h.Value, Is.EqualTo(3));
            Assert.That(h.AppliedSelection, Is.EqualTo(CscSelectionSnapshot.Element(9)));
        }

        [Test]
        public void A_gesture_that_changes_nothing_records_nothing()
        {
            var h = new Harness();
            h.History.BeginGesture();
            Assert.That(h.History.EndGesture(), Is.False);
            Assert.That(h.History.CanUndo, Is.False);
        }

        [Test]
        public void Cancelling_a_gesture_restores_the_scene_and_records_nothing()
        {
            var h = new Harness();
            h.Edit(1);
            h.History.BeginGesture();
            h.Edit(4);

            h.History.CancelGesture();

            Assert.That(h.Value, Is.EqualTo(1));
            Assert.That(h.History.HasUnsavedChanges, Is.True);
            h.History.Undo();
            Assert.That(h.Value, Is.EqualTo(0));
            Assert.That(h.History.CanUndo, Is.False);
        }

        [Test]
        public void History_keeps_only_the_most_recent_steps()
        {
            var h = new Harness();
            for (var i = 1; i <= CscEditHistory.MaxSteps + 5; i++)
                h.Edit((byte)i);

            var undone = 0;
            while (h.History.Undo())
                undone++;

            Assert.That(undone, Is.EqualTo(CscEditHistory.MaxSteps));
            Assert.That(h.Value, Is.EqualTo(5));
        }

        [Test]
        public void An_unrecorded_change_still_counts_as_unsaved()
        {
            var h = new Harness();
            h.History.MarkUnrecordedChange();
            Assert.That(h.History.HasUnsavedChanges, Is.True);
            Assert.That(h.History.CanUndo, Is.False);
        }

        [Test]
        public void Loading_a_document_clears_the_history()
        {
            var h = new Harness();
            h.Edit(1);
            h.History.InitializeLoadedDocument([9]);
            Assert.That(h.History.CanUndo, Is.False);
            Assert.That(h.History.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void Undoing_a_real_scene_edit_restores_the_written_scene()
        {
            var original = CscSceneRoundTripTests.BuildSceneWithManifestBytes(elementCount: 2);
            var scene = CscScene.Load(original);
            var history = new CscEditHistory(
                () => CscSceneWriter.Write(scene),
                () => CscSelectionSnapshot.None,
                (bytes, _) => scene = CscScene.Load(bytes));
            history.InitializeLoadedDocument(original);

            scene.Duration += 3;
            history.RecordChange();
            scene.RemoveElementSubtree(scene.Elements[0]);
            history.RecordChange();
            Assert.That(scene.Elements, Has.Count.EqualTo(1));

            history.Undo();
            Assert.That(scene.Elements, Has.Count.EqualTo(2));
            Assert.That(scene.Duration, Is.EqualTo(23f));

            history.Undo();
            Assert.That(scene.Duration, Is.EqualTo(20f));
            Assert.That(CscSceneWriter.Write(scene), Is.EqualTo(CscSceneWriter.Write(CscScene.Load(original))));
        }
    }
}
