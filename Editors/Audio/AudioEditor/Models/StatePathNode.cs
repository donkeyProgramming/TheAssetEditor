namespace Editors.Audio.AudioEditor.Models
{
    public class StatePathNode
    {
        public StateGroup StateGroup { get; set; }
        public State State { get; set; }

        public static StatePathNode Create(StateGroup stateGroup, State state)
        {
            return new StatePathNode
            {
                StateGroup = stateGroup,
                State = state
            };
        }
    }
}
