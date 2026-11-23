using System;
using System.Collections.Generic;

namespace Editors.CscEditor.Services
{
    /// <summary>What was selected when a snapshot was taken, restored with it on undo/redo.</summary>
    public readonly record struct CscSelectionSnapshot(bool IsSceneRoot, int? ElementId)
    {
        public static CscSelectionSnapshot None => new(false, null);
        public static CscSelectionSnapshot Root => new(true, null);
        public static CscSelectionSnapshot Element(int id) => new(false, id);
    }

    /// <summary>
    /// Undo/redo for the CSC editor. Each step stores the whole scene as written .csc bytes before
    /// and after the change, so any edit (fields, curves, gizmo, structure) undoes the same way and
    /// an undone scene is exactly what would have been saved.
    /// <para/>
    /// A gesture (a mouse drag, or a structural edit made of several data changes) becomes one
    /// step: changes recorded while it is active are folded into the step made when it ends.
    /// </summary>
    public sealed class CscEditHistory
    {
        public const int MaxSteps = 100;

        readonly Func<byte[]> _captureSnapshot;
        readonly Func<CscSelectionSnapshot> _captureSelection;
        readonly Action<byte[], CscSelectionSnapshot> _applySnapshot;

        readonly LinkedList<Step> _undo = new();
        readonly Stack<Step> _redo = new();

        byte[]? _current;
        long _currentStateId;
        long _lastStateId;
        long _savedStateId;

        bool _gestureActive;
        byte[]? _gestureStartSnapshot;
        long _gestureStartStateId;
        CscSelectionSnapshot _gestureStartSelection;

        public CscEditHistory(
            Func<byte[]> captureSnapshot,
            Func<CscSelectionSnapshot> captureSelection,
            Action<byte[], CscSelectionSnapshot> applySnapshot)
        {
            _captureSnapshot = captureSnapshot;
            _captureSelection = captureSelection;
            _applySnapshot = applySnapshot;
        }

        public bool CanUndo => !_gestureActive && _undo.Count > 0;
        public bool CanRedo => !_gestureActive && _redo.Count > 0;
        public bool HasUnsavedChanges => _currentStateId != _savedStateId;
        public bool IsGestureActive => _gestureActive;

        /// <summary>Starts a fresh history for a newly loaded document. <paramref name="loadedBytes"/>
        /// are the file as loaded, so undoing every step returns to exactly that file.</summary>
        public void InitializeLoadedDocument(byte[] loadedBytes)
        {
            _current = (byte[])loadedBytes.Clone();
            _undo.Clear();
            _redo.Clear();
            _currentStateId = _lastStateId = _savedStateId = 0;
            _gestureActive = false;
            _gestureStartSnapshot = null;
        }

        public void MarkSaved() => _savedStateId = _currentStateId;

        /// <summary>Marks the document changed when the change could not be captured as a
        /// snapshot. No step is recorded; the next successful one starts from the last good
        /// snapshot.</summary>
        public void MarkUnrecordedChange() => _currentStateId = ++_lastStateId;

        public void BeginGesture()
        {
            if (_current == null || _gestureActive)
                return;

            _gestureActive = true;
            _gestureStartSnapshot = _current;
            _gestureStartStateId = _currentStateId;
            _gestureStartSelection = _captureSelection();
        }

        /// <summary>Ends the active gesture, recording one step if the scene changed.</summary>
        public bool EndGesture()
        {
            if (!_gestureActive)
                return false;

            _gestureActive = false;
            _gestureStartSnapshot = null;
            return Record(_gestureStartSelection);
        }

        /// <summary>Ends the active gesture and puts the scene back as it was when it began.</summary>
        public void CancelGesture()
        {
            if (!_gestureActive)
                return;

            _gestureActive = false;
            var snapshot = _gestureStartSnapshot!;
            _gestureStartSnapshot = null;
            _currentStateId = _gestureStartStateId;
            _current = snapshot;
            _applySnapshot((byte[])snapshot.Clone(), _gestureStartSelection);
        }

        /// <summary>Records one step for a change that has already been made to the scene.
        /// Inside a gesture this does nothing; the gesture records when it ends.</summary>
        public bool RecordChange()
        {
            if (_gestureActive)
                return false;
            return Record(_captureSelection());
        }

        bool Record(CscSelectionSnapshot selectionBefore)
        {
            if (_current == null)
                return false;

            var next = _captureSnapshot();
            if (_current.AsSpan().SequenceEqual(next))
                return false;

            var stateId = ++_lastStateId;
            _undo.AddLast(new Step(_current, next, _currentStateId, stateId, selectionBefore, _captureSelection()));
            if (_undo.Count > MaxSteps)
                _undo.RemoveFirst();
            _redo.Clear();

            _current = next;
            _currentStateId = stateId;
            return true;
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;

            var step = _undo.Last!.Value;
            _undo.RemoveLast();
            _redo.Push(step);
            Apply(step.Before, step.BeforeStateId, step.SelectionBefore);
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo)
                return false;

            var step = _redo.Pop();
            _undo.AddLast(step);
            Apply(step.After, step.AfterStateId, step.SelectionAfter);
            return true;
        }

        void Apply(byte[] snapshot, long stateId, CscSelectionSnapshot selection)
        {
            _current = snapshot;
            _currentStateId = stateId;
            _applySnapshot((byte[])snapshot.Clone(), selection);
        }

        sealed record Step(
            byte[] Before,
            byte[] After,
            long BeforeStateId,
            long AfterStateId,
            CscSelectionSnapshot SelectionBefore,
            CscSelectionSnapshot SelectionAfter);
    }
}
