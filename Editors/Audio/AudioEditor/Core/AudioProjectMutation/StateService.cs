using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.AudioEditor.Core.AudioProjectMutation
{
    public interface IStateService
    {
        void AddState(string stateGroupName, string stateName);
        void RemoveState(string stateGroupName, string stateName);
    }

    public class StateService(IAudioEditorStateService audioEditorStateService) : IStateService
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;

        public void AddState(string stateGroupName, string stateName)
        {
            var stateGroup = _audioEditorStateService.AudioProject.GetStateGroup(stateGroupName);
            var state = State.Create(stateName);
            stateGroup.States.InsertAlphabetically(state);
        }

        public void RemoveState(string stateGroupName, string stateName)
        {
            var stateGroup = _audioEditorStateService.AudioProject.GetStateGroup(stateGroupName);
            var state = stateGroup.GetState(stateName);
            stateGroup.States.Remove(state);
        }
    }
}
