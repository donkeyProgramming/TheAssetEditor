using System;
using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Enums;
using Action = Editors.Audio.Shared.AudioProject.Models.Action;

namespace Editors.Audio.Shared.AudioProject.Factories
{
    public record ActionEventFactoryResult
    {
        public ActionEvent ActionEvent { get; set; }
        public List<Action> Actions { get; set; }
        public Sound SoundTarget { get; set; }
        public RandomSequenceContainer RandomSequenceContainerTarget { get; set; }
        public List<Sound> RandomSequenceContainerSounds { get; set; } = [];
    }

    public interface IActionEventFactory
    {
        ActionEventFactoryResult CreatePlayActionEvent(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            Wh3ActionEventType actionEventGroup,
            string actionEventName,
            List<AudioFile> audioFiles,
            HircSettings hircSettings,
            uint soundBankId,
            string language);
        ActionEventFactoryResult CreatePauseActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent);
        ActionEventFactoryResult CreateResumeActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent);
        ActionEventFactoryResult CreateStopActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent);
    }

    public class ActionEventFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory) : IActionEventFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory = randomSequenceContainerFactory;

        public ActionEventFactoryResult CreatePlayActionEvent(
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            Wh3ActionEventType actionEventType,
            string actionEventName,
            List<AudioFile> audioFiles,
            HircSettings hircSettings,
            uint soundBankId,
            string language)
        {
            var actionEventFactoryResult = new ActionEventFactoryResult();
            var actions = new List<Action>();

            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, actionEventName);
            var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(actionEventType);
            var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(actionEventType);

            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(usedHircIds, usedSourceIds, audioFiles[0], hircSettings, language, overrideBusId: overrideBusId, directParentId: actorMixerId);
                actionEventFactoryResult.SoundTarget = sound;

                var actionIds = IdGenerator.GenerateIds(usedHircIds);
                var playAction = Action.Create(actionIds.Id, AkBkHircType.Sound, AkActionType.Play, sound.Id, soundBankId);

                actions.Add(playAction);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainerResult = _randomSequenceContainerFactory.Create(usedHircIds, usedSourceIds, hircSettings, audioFiles, language, overrideBusId, actorMixerId);
                actionEventFactoryResult.RandomSequenceContainerTarget = randomSequenceContainerResult.RandomSequenceContainer;
                actionEventFactoryResult.RandomSequenceContainerSounds.AddRange(randomSequenceContainerResult.RandomSequenceContainerSounds);

                var actionIds = IdGenerator.GenerateIds(usedHircIds);
                var playAction = Action.Create(actionIds.Id, AkBkHircType.RandomSequenceContainer, AkActionType.Play, actionEventFactoryResult.RandomSequenceContainerTarget.Id, soundBankId);
                actions.Add(playAction);
            }

            var actionEvent = ActionEvent.Create(actionEventId, actionEventName, actions, actionEventType);
            actionEventFactoryResult.ActionEvent = actionEvent;
            actionEventFactoryResult.Actions = actions;
            return actionEventFactoryResult;
        }

        public ActionEventFactoryResult CreatePauseActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent)
        {
            var pauseActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                if (playAction.TargetHircTypeIsSound())
                {
                    var actionIds = IdGenerator.GenerateIds(usedHircIds);
                    var pauseAction = Action.Create(actionIds.Id, playAction.TargetHircType, AkActionType.Pause_E_O, playAction.IdExt, playAction.BankId);
                    pauseActions.Add(pauseAction);
                }
                else if (playAction.TargetHircTypeIsRandomSequenceContainer())
                {
                    var actionIds = IdGenerator.GenerateIds(usedHircIds);
                    var pauseAction = Action.Create(actionIds.Id, playAction.TargetHircType, AkActionType.Pause_E_O, playAction.IdExt, playAction.BankId);
                    pauseActions.Add(pauseAction);
                }
            }

            var pauseActionEventName = string.Concat("Pause_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, pauseActionEventName);
            var actionEvent = ActionEvent.Create(actionEventId, pauseActionEventName, pauseActions, playActionEvent.ActionEventType);
            return new ActionEventFactoryResult { ActionEvent = actionEvent };
        }


        public ActionEventFactoryResult CreateResumeActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent)
        {
            var resumeActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                if (playAction.TargetHircTypeIsSound())
                {
                    var actionIds = IdGenerator.GenerateIds(usedHircIds);
                    var resumeAction = Action.Create(actionIds.Id, playAction.TargetHircType, AkActionType.Resume_E_O, playAction.IdExt, playAction.BankId);
                    resumeActions.Add(resumeAction);
                }
                else if (playAction.TargetHircTypeIsRandomSequenceContainer())
                {
                    var actionIds = IdGenerator.GenerateIds(usedHircIds);
                    var resumeAction = Action.Create(actionIds.Id, playAction.TargetHircType, AkActionType.Resume_E_O, playAction.IdExt, playAction.BankId);
                    resumeActions.Add(resumeAction);
                }
            }

            var resumeActionEventName = string.Concat("Resume_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, resumeActionEventName);
            var actionEvent = ActionEvent.Create(actionEventId, resumeActionEventName, resumeActions, playActionEvent.ActionEventType);
            return new ActionEventFactoryResult { ActionEvent = actionEvent };
        }

        public ActionEventFactoryResult CreateStopActionEvent(HashSet<uint> usedHircIds, ActionEvent playActionEvent)
        {
            var stopActions = new List<Action>();

            var playActions = playActionEvent.GetPlayActions();
            foreach (var playAction in playActions)
            {
                if (playAction.TargetHircTypeIsSound())
                {
                    var actionIds = IdGenerator.GenerateIds(usedHircIds);
                    var stopAction = Action.Create(actionIds.Id, playAction.TargetHircType, AkActionType.Stop_E_O, playAction.IdExt, playAction.BankId);
                    stopActions.Add(stopAction);
                }
                else if (playAction.TargetHircTypeIsRandomSequenceContainer())
                {
                    var actionIds = IdGenerator.GenerateIds(usedHircIds);
                    var stopAction = Action.Create(actionIds.Id, playAction.TargetHircType, AkActionType.Stop_E_O, playAction.IdExt, playAction.BankId);
                    stopActions.Add(stopAction);
                }
            }

            var stopActionEventName = string.Concat("Stop_", playActionEvent.Name.AsSpan("Play_".Length));
            var actionEventId = IdGenerator.GenerateActionEventId(usedHircIds, stopActionEventName);
            var actionEvent = ActionEvent.Create(actionEventId,stopActionEventName, stopActions, playActionEvent.ActionEventType);
            return new ActionEventFactoryResult { ActionEvent = actionEvent };
        }
    }
}
