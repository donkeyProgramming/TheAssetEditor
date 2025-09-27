using System;
using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Action = Editors.Audio.AudioEditor.Models.Action;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IActionEventFactory
    {
        ActionEvent CreatePlayActionEvent(Wh3ActionEventType actionEventGroup, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings);
        ActionEvent CreateResumeActionEvent(ActionEvent playActionEvent);
        ActionEvent CreatePauseActionEvent(ActionEvent playActionEvent);
        ActionEvent CreateStopActionEvent(ActionEvent playActionEvent);
    }

    public class ActionEventFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory) : IActionEventFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory = randomSequenceContainerFactory;

        public ActionEvent CreatePlayActionEvent(Wh3ActionEventType actionEventType, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var actions = new List<Action>();

            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(audioFiles[0], audioSettings);
                var playAction = Action.Create(sound, AkActionType.Play);
                actions.Add(playAction);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainer = _randomSequenceContainerFactory.Create(audioSettings, audioFiles);
                var playAction = Action.Create(randomSequenceContainer, AkActionType.Play);
                actions.Add(playAction);
            }

            var actionEvent = ActionEvent.Create(actionEventName, actions, actionEventType);
            return actionEvent;
        }

        public ActionEvent CreateResumeActionEvent(ActionEvent playActionEvent)
        {
            var resumeActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                if (playAction.Sound != null)
                {
                    var resumeAction = Action.Create(playAction.Sound, AkActionType.Resume_E_O);
                    resumeActions.Add(resumeAction);
                }
                else if (playAction.RandomSequenceContainer != null)
                {
                    var resumeAction = Action.Create(playAction.RandomSequenceContainer, AkActionType.Resume_E_O);
                    resumeActions.Add(resumeAction);
                }
            }

            var resumeActionEventName = string.Concat("Resume_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEvent = ActionEvent.Create(resumeActionEventName, resumeActions, playActionEvent.ActionEventType);
            return actionEvent;
        }

        public ActionEvent CreatePauseActionEvent(ActionEvent playActionEvent)
        {
            var pauseActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                if (playAction.Sound != null)
                {
                    var pauseAction = Action.Create(playAction.Sound, AkActionType.Pause_E_O);
                    pauseActions.Add(pauseAction);
                }
                else if (playAction.RandomSequenceContainer != null)
                {
                    var pauseAction = Action.Create(playAction.RandomSequenceContainer, AkActionType.Pause_E_O);
                    pauseActions.Add(pauseAction);
                }
            }

            var pauseActionEventName = string.Concat("Pause_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEvent = ActionEvent.Create(pauseActionEventName, pauseActions, playActionEvent.ActionEventType);
            return actionEvent;
        }

        public ActionEvent CreateStopActionEvent(ActionEvent playActionEvent)
        {
            var stopActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            { 
                if (playAction.Sound != null)
                {
                    var stopAction = Action.Create(playAction.Sound, AkActionType.Stop_E_O);
                    stopActions.Add(stopAction);
                }
                else if (playAction.RandomSequenceContainer != null)
                {
                    var stopAction = Action.Create(playAction.RandomSequenceContainer, AkActionType.Stop_E_O);
                    stopActions.Add(stopAction);
                }
            }

            var stopActionEventName = string.Concat("Stop_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEvent = ActionEvent.Create(stopActionEventName, stopActions, playActionEvent.ActionEventType);
            return actionEvent;
        }
    }
}
