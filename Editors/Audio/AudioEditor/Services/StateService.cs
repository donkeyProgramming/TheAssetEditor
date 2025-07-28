using Editors.Audio.AudioEditor.Models;

namespace Editors.Audio.AudioEditor.Services
{
    public interface IStateService
    {
        void AddState(string stateGroupName, string stateName);
        void RemoveState(string stateGroupName, string stateName);
    }

    public class StateService(IAudioEditorService audioEditorService) : IStateService
    {
        private readonly IAudioEditorService _audioEditorService = audioEditorService;

        public void AddState(string stateGroupName, string stateName)
        {
            var stateGroup = _audioEditorService.AudioProject.GetStateGroup(stateGroupName);
            var state = State.Create(stateName);
            stateGroup.InsertAlphabetically(state);
        }

        public void RemoveState(string stateGroupName, string stateName)
        {
            var stateGroup = _audioEditorService.AudioProject.GetStateGroup(stateGroupName);
            var state = stateGroup.GetState(stateName);
            stateGroup.States.Remove(state);
        }
    }
}
