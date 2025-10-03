using System;
using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Action = Editors.Audio.AudioEditor.Models.Action;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IActionEventFactory
    {
        ActionEvent CreatePlayActionEvent(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, Wh3ActionEventType actionEventGroup, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings);
        ActionEvent CreateResumeActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent);
        ActionEvent CreatePauseActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent);
        ActionEvent CreateStopActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent);
    }

    public class ActionEventFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory) : IActionEventFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory = randomSequenceContainerFactory;

        public ActionEvent CreatePlayActionEvent(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, Wh3ActionEventType actionEventType, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var actions = new List<Action>();
            var actionName = $"{actionEventName}_action";
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, actionEventName);
            var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(actionEventType);
            var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(actionEventType);

            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFiles[0], audioSettings, overrideBusId: overrideBusId, directParentId: actorMixerId);
                var id = IdGenerator.GenerateActionId(usedHircIds, actionName, actionEventName);
                var playAction = Action.Create(id, actionName, sound, AkActionType.Play, sound.Id);
                actions.Add(playAction);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainer = _randomSequenceContainerFactory.Create(usedHircIds, usedSourceIds, audioSettings, audioFiles, overrideBusId, actorMixerId);
                var id = IdGenerator.GenerateActionId(usedHircIds, actionName, actionEventName);
                var playAction = Action.Create(id, actionName, randomSequenceContainer, AkActionType.Play, randomSequenceContainer.Id);
                actions.Add(playAction);
            }

            var actionEvent = ActionEvent.Create(actionEventId, actionEventName, actions, actionEventType);
            return actionEvent;
        }

        public ActionEvent CreateResumeActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent)
        {
            var resumeActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                var actionName = $"{playAction.Name}_action";
                if (playAction.Sound != null)
                {
                    var id = IdGenerator.GenerateActionId(usedHircIds, actionName, playActionEvent.Name);
                    var resumeAction = Action.Create(id, actionName, playAction.Sound, AkActionType.Resume_E_O, playAction.Sound.Id);
                    resumeActions.Add(resumeAction);
                }
                else if (playAction.RandomSequenceContainer != null)
                {
                    var id = IdGenerator.GenerateActionId(usedHircIds, actionName, playActionEvent.Name);
                    var resumeAction = Action.Create(id, actionName, playAction.RandomSequenceContainer, AkActionType.Resume_E_O, playAction.RandomSequenceContainer.Id);
                    resumeActions.Add(resumeAction);
                }
            }

            var resumeActionEventName = string.Concat("Resume_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, resumeActionEventName);
            var actionEvent = ActionEvent.Create(actionEventId, resumeActionEventName, resumeActions, playActionEvent.ActionEventType);
            return actionEvent;
        }

        public ActionEvent CreatePauseActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent)
        {
            var pauseActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                var actionName = $"{playAction.Name}_action";
                if (playAction.Sound != null)
                {
                    var id = IdGenerator.GenerateActionId(usedHircIds, actionName, playActionEvent.Name);
                    var pauseAction = Action.Create(id, actionName, playAction.Sound, AkActionType.Pause_E_O, playAction.Sound.Id);
                    pauseActions.Add(pauseAction);
                }
                else if (playAction.RandomSequenceContainer != null)
                {
                    var id = IdGenerator.GenerateActionId(usedHircIds, actionName, playActionEvent.Name);
                    var pauseAction = Action.Create(id, actionName, playAction.RandomSequenceContainer, AkActionType.Pause_E_O, playAction.RandomSequenceContainer.Id);
                    pauseActions.Add(pauseAction);
                }
            }

            var pauseActionEventName = string.Concat("Pause_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, pauseActionEventName);
            var actionEvent = ActionEvent.Create(actionEventId, pauseActionEventName, pauseActions, playActionEvent.ActionEventType);
            return actionEvent;
        }

        public ActionEvent CreateStopActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent)
        {
            var stopActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                var actionName = $"{playAction.Name}_action";
                if (playAction.Sound != null)
                {
                    var id = IdGenerator.GenerateActionId(usedHircIds, actionName, playActionEvent.Name);
                    var stopAction = Action.Create(id, actionName, playAction.Sound, AkActionType.Stop_E_O, playAction.Sound.Id);
                    stopActions.Add(stopAction);
                }
                else if (playAction.RandomSequenceContainer != null)
                {
                    var id = IdGenerator.GenerateActionId(usedHircIds, actionName, playActionEvent.Name);
                    var stopAction = Action.Create(id, actionName, playAction.RandomSequenceContainer, AkActionType.Stop_E_O, playAction.RandomSequenceContainer.Id);
                    stopActions.Add(stopAction);
                }
            }

            var stopActionEventName = string.Concat("Stop_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, stopActionEventName);
            var actionEvent = ActionEvent.Create(actionEventId,stopActionEventName, stopActions, playActionEvent.ActionEventType);
            return actionEvent;
        }
    }
}
