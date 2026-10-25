using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Factories;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.Shared.AudioProject
{
    public record MusicAudioProjectResult(
        string FilePath,
        IReadOnlyList<string> CreatedEvents,
        IReadOnlyList<(string EventName, string Reason)> SkippedEvents);

    /// <summary>
    /// Builds an Audio Project containing one music Action Event per named event, shaped the way
    /// vanilla shapes them.
    ///
    /// A vanilla music event is a single SetState action against one of the music State Groups -
    /// music_b_faction_empire sets Battle_Music_WH3_Culture to Empire - and holds no Sound, no
    /// container and no audio file. What makes a State audible is the music hierarchy branching on
    /// it, which lives in the game's own banks; the event's whole job is to select a branch.
    ///
    /// So this generates the events and the States they select and stops there. The audio comes
    /// later, in the Audio Editor, where picking wavs for an event builds the branch its State
    /// selects - and only for State Groups a mod can actually extend, which of the six these events
    /// target is just WH3_Campaign_Subcultures. See
    /// <see cref="Wh3MusicHierarchyInformation.GetMusicSwitchContainerIds"/> for why.
    /// </summary>
    public interface IMusicAudioProjectService
    {
        MusicAudioProjectResult CreateForMusicEvents(string projectName, string directory, IReadOnlyList<string> eventNames);
    }

    public class MusicAudioProjectService(
        IPackFileService packFileService,
        IAudioProjectFileService audioProjectFileService,
        IActionEventFactory actionEventFactory,
        IAudioRepository audioRepository) : IMusicAudioProjectService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IAudioProjectFileService _audioProjectFileService = audioProjectFileService;
        private readonly IActionEventFactory _actionEventFactory = actionEventFactory;
        private readonly IAudioRepository _audioRepository = audioRepository;

        private readonly ILogger _logger = Logging.Create<MusicAudioProjectService>();

        public MusicAudioProjectResult CreateForMusicEvents(string projectName, string directory, IReadOnlyList<string> eventNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);
            ArgumentNullException.ThrowIfNull(eventNames);

            if (_packFileService.GetEditablePack() == null)
                throw new InvalidOperationException("No editable pack is loaded, so the audio project has nowhere to be written.");

            // Music always lives in the Sfx language folder regardless of what a project is
            // otherwise set to (global_music declares Sfx as its RequiredLanguage), so the
            // project is created as Sfx outright - that keeps the ID uniqueness scope, which
            // is per language, the same one the SoundBank will actually be compiled into.
            var language = Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx);
            var audioProject = AudioProjectFile.CreateNew(language, projectName);

            // Populates the repository so GenerateActionEventId can see vanilla IDs and refuse
            // a name that already exists - adding a culture called "empire" would otherwise
            // silently produce a second music_b_faction_empire shadowing the real one. Failure
            // here is not fatal: without game files the repository simply reports nothing used,
            // which costs the collision check but still yields a valid project.
            TryLoadAudioRepository(language);

            var soundBankName = $"{Wh3SoundBankInformation.GetName(Wh3SoundBank.GlobalMusic)}_{projectName}";
            var soundBank = audioProject.GetSoundBank(soundBankName)
                ?? throw new InvalidOperationException($"Newly created audio project has no '{soundBankName}' SoundBank.");

            var created = new List<string>();
            var skipped = new List<(string, string)>();

            foreach (var eventName in eventNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    AddMusicEvent(audioProject, soundBank, eventName);
                    created.Add(eventName);
                }
                catch (Exception e)
                {
                    // Most likely GenerateActionEventId rejecting a name that already exists in
                    // vanilla. One unusable event should not cost the modder the other five, so
                    // it is reported back rather than aborting the whole project.
                    _logger.Here().Error($"Could not add music event '{eventName}': {e.Message}");
                    skipped.Add((eventName, e.Message));
                }
            }

            var fileName = $"{projectName}.aproj";
            var filePath = $"{directory}\\{fileName}";
            _audioProjectFileService.Save(audioProject, fileName, filePath);

            return new MusicAudioProjectResult(filePath, created, skipped);
        }

        private void AddMusicEvent(AudioProjectFile audioProject, SoundBank soundBank, string eventName)
        {
            if (!Wh3MusicEventInformation.TryResolveStateTarget(eventName, out var stateGroupName, out var stateName))
                throw new InvalidOperationException(
                    $"'{eventName}' does not follow any of the vanilla music event patterns ({string.Join(", ", Wh3MusicEventInformation.KnownEventPrefixes)}), " +
                    "so there is no way to tell which State Group it should set.");

            var usedHircIds = IdGenerator.GetUsedHircIds(_audioRepository, audioProject);

            var result = _actionEventFactory.CreateSetStateActionEvent(
                usedHircIds,
                Wh3ActionEventType.Music,
                eventName,
                stateGroupName,
                stateName);

            soundBank.ActionEvents.InsertAlphabetically(result.ActionEvent);
            foreach (var stateGroupToRegisterIn in Wh3StateGroupInformation.GetStateGroupsToRegisterIn(stateGroupName))
                AddStateToStateGroup(audioProject, stateGroupToRegisterIn, stateName);
        }

        /// <summary>
        /// Registers the State the event selects, so it shows up in the Audio Explorer and in the
        /// generated .dat. Vanilla States are already known to the game, so re-adding one would
        /// only produce a duplicate.
        /// </summary>
        private void AddStateToStateGroup(AudioProjectFile audioProject, string stateGroupName, string stateName)
        {
            var stateGroup = audioProject.StateGroups.FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);
            if (stateGroup == null)
                return;

            if (stateGroup.States.Any(state => StringComparer.OrdinalIgnoreCase.Equals(state.Name, stateName)))
                return;

            if (_audioRepository.StatesByStateGroup.TryGetValue(stateGroupName, out var vanillaStates) &&
                vanillaStates.Contains(stateName, StringComparer.OrdinalIgnoreCase))
                return;

            stateGroup.States.InsertAlphabetically(new State(stateName));
        }

        private void TryLoadAudioRepository(string language)
        {
            try
            {
                _audioRepository.Load([language]);
            }
            catch (Exception e)
            {
                _logger.Here().Warning($"Could not load audio data, so generated event names cannot be checked against vanilla: {e.Message}");
            }
        }
    }
}
