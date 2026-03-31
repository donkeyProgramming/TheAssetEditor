using CommunityToolkit.Diagnostics;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Serilog;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Commands.Object
{
    public class ObjectSelectionCommand : ICommand
    {
        private readonly ILogger _logger = Logging.Create<ObjectSelectionCommand>();
        private readonly SelectionManager _selectionManager;

        ISelectionState? _oldState;
        bool _isModification;
        bool _isRemove;
        List<ISelectable> _items = [];

        public string HintText { get => "Object Selected"; }
        public bool IsMutation { get => false; }

        public ObjectSelectionCommand(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;
        }

        public void Configure(List<ISelectable> items, bool isModification = false, bool removeSelection = false)
        {
            _items = items;
            _isModification = isModification;
            _isRemove = removeSelection;
        }

        public void Configure(ISelectable item, bool isModification = false, bool removeSelection = false)
        {
            _items = new List<ISelectable>() { item };
            _isModification = isModification;
            _isRemove = removeSelection;
        }

        public void Execute()
        {
            _oldState = _selectionManager.GetStateCopy();
            var currentState = _selectionManager.GetState() as ObjectSelectionState;
            if (currentState == null)
                currentState = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null) as ObjectSelectionState;

            Guard.IsNotNull(currentState);
            _logger.Here().Information($"Command info - Remove[{_isRemove}] Mod[{_isModification}] Items[{string.Join(',', _items.Select(x => x.Name))}]");

            if (!(_isModification || _isRemove))
                currentState.Clear();

            currentState.ModifySelection(_items, _isRemove);
        }

        public void Undo()
        {
            Guard.IsNotNull(_oldState);
            _selectionManager.SetState(_oldState);
        }
    }
}


