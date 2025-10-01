using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Utility;

namespace Editors.Audio.AudioEditor.Models
{
    public partial class SoundBank : AudioProjectItem
    {
        public string Language { get; set; }
        public uint LanguageId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public uint TestingId { get; set; }
        public string TestingFileName { get; set; }
        public string TestingFilePath { get; set; }
        public uint MergingId { get; set; }
        public string MergingFileName { get; set; }
        public string MergingFilePath { get; set; }
        public Wh3SoundBank GameSoundBank { get; set; }
        public List<ActionEvent> ActionEvents { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; }

        public static SoundBank Create(string name, Wh3SoundBank gameSoundBank, string language)
        {
            return new SoundBank
            {
                Id = WwiseHash.Compute(name),
                Name = name,
                GameSoundBank = gameSoundBank,
                Language = language,
                LanguageId = WwiseHash.Compute(language)
            };
        }

        public ActionEvent GetActionEvent(string actionEventName) => ActionEvents.FirstOrDefault(actionEvent => actionEvent.Name == actionEventName);

        public List<Wh3ActionEventType> GetUsedActionEventTypes()
        {
            var usedActionEventGroups = ActionEvents
                .Select(actionEventGroup => actionEventGroup.ActionEventType)
                .Distinct();

            var allowedActionEventGroups = Wh3ActionEventInformation
                .GetSoundBankActionEventTypes(GameSoundBank);

            return usedActionEventGroups
                .Where(actionEventGroup => allowedActionEventGroups.Contains(actionEventGroup))
                .OrderBy(actionEventGroup => allowedActionEventGroups.IndexOf(actionEventGroup))
                .ToList();
        }

        public List<DialogueEvent> GetEditedDialogueEvents()
        {
            return DialogueEvents
                .Where(dialogueEvent => dialogueEvent.StatePaths.Count != 0)
                .ToList();
        }

        public void InsertAlphabetically(ActionEvent actionEvent) => InsertAlphabeticallyUnique(ActionEvents, actionEvent);

        public List<ActionEvent> GetPlayActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetPlayActions().Count != 0 
                    && actionEvent.GetResumeActions().Count == 0 
                    && actionEvent.GetPauseActions().Count == 0 
                    && actionEvent.GetStopActions().Count == 0)
                .ToList()
                ?? [];
        }

        public List<ActionEvent> GetPauseActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetPauseActions().Count != 0
                    && actionEvent.GetPlayActions().Count == 0
                    && actionEvent.GetResumeActions().Count == 0
                    && actionEvent.GetStopActions().Count == 0)
                .ToList()
                ?? [];
        }

        public List<ActionEvent> GetResumeActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetResumeActions().Count != 0
                    && actionEvent.GetPlayActions().Count == 0
                    && actionEvent.GetPauseActions().Count == 0
                    && actionEvent.GetStopActions().Count == 0)
                .ToList()
                ?? [];
        }

        public List<ActionEvent> GetStopActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetStopActions().Count != 0
                    && actionEvent.GetPlayActions().Count == 0
                    && actionEvent.GetResumeActions().Count == 0 
                    && actionEvent.GetPauseActions().Count == 0)
                .ToList()
                ?? [];
        }

        public List<Action> GetActions()
        {
            return (ActionEvents ?? [])
                .Where(actionEvent => actionEvent?.Actions != null)
                .SelectMany(actionEvent => actionEvent.Actions)
                .ToList();
        }

        public List<StatePath> GetStatePaths()
        {
            return (DialogueEvents ?? [])
                .Where(dialogueEvent => dialogueEvent?.StatePaths != null)
                .SelectMany(dialogueEvent => dialogueEvent.StatePaths)
                .ToList();
        }

        public List<Sound> GetActionSounds()
        {
            var actions = GetActions();
            var actionSounds = actions.Select(action => action.Sound).ToList();
            var statePaths = GetStatePaths();
            var statePathSounds = statePaths.Select(statePath => statePath.Sound).ToList();
            return [..actionSounds, ..statePathSounds];
        }

        public List<Sound> GetRandomSequenceContainerSounds()
        {
            var actions = GetActions();
            var actionSounds = actions
                .Where(action => action?.RandomSequenceContainer?.Sounds != null)
                .SelectMany(action => action.RandomSequenceContainer.Sounds);

            var statePaths = GetStatePaths();
            var statePathSounds = statePaths
                .Where(statePath => statePath?.RandomSequenceContainer?.Sounds != null)
                .SelectMany(statePath => statePath.RandomSequenceContainer.Sounds);

            return [.. actionSounds, .. statePathSounds];
        }

        public List<Sound> GetSounds()
        {
            var actionSounds = GetActionSounds();
            var statePathSounds = GetRandomSequenceContainerSounds();
            return [..actionSounds, ..statePathSounds];
        }

        public ActionEvent GetPlayActionEventFromPauseActionEventName(string pauseActionEventName)
        {
            var playActionEventName = string.Concat("Play_", pauseActionEventName.AsSpan("Pause_".Length));
            return GetActionEvent(playActionEventName);
        }

        public ActionEvent GetPlayActionEventFromResumeActionEventName(string resumeActionEventName)
        {
            var playActionEventName = string.Concat("Play_", resumeActionEventName.AsSpan("Resume_".Length));
            return GetActionEvent(playActionEventName);
        }

        public ActionEvent GetPlayActionEventFromStopActionEventName(string stopActionEventName)
        {
            var playActionEventName = string.Concat("Play_", stopActionEventName.AsSpan("Stop_".Length));
            return GetActionEvent(playActionEventName);
        }
    }
}
