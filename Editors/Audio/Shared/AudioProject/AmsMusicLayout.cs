using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.HircExploration;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;

namespace Editors.Audio.Shared.AudioProject
{
    /// <summary>One vanilla pulse track: the musical key the campaign score is in when it plays,
    /// and which of that key's segments it sits under, counting from 1.</summary>
    public record AmsPulseSlot(CAkMusicTrack_V136 Track, string Key, int Variation);

    /// <summary>
    /// How the adaptive music system's two culture layers are laid out in the game's banks, read
    /// from them rather than listed, so the editor, the compile and the vanilla reference agree on
    /// what a key or a variation is.
    ///
    /// Both layers play over the campaign score and follow its musical key, the State Group
    /// <see cref="KeyStateGroupName"/>, whose values come in threes - C_01, C_02, C_03 and so on.
    /// Vanilla never tells the three apart: every culture's fragments point all three at one branch,
    /// and the pulse container sends all three to one playlist. So a key here is the part before
    /// the underscore - C, Eb, Gb - and "None" is no key at all.
    ///
    ///   pulses     a Music Switch container on the key, one playlist of three segments per key,
    ///              each segment carrying one switch track per pulse Group. The nine tracks per Group
    ///              are three keys by three variations.
    ///   fragments  the faction Switch container, then per culture a Switch on the key, then a Switch
    ///              on <see cref="CallResponseStateGroupName"/> - Call or Response - over the Sounds.
    ///
    /// Read lazily and cached; the repository is not safe to read from two threads.
    /// </summary>
    public interface IAmsMusicLayout
    {
        IReadOnlyList<AmsPulseSlot> GetPulseSlots(string stateGroupName);

        /// <summary>The keys vanilla's fragments are written in, in the order the game lists them.</summary>
        IReadOnlyList<string> GetFragmentKeys(uint fragmentContainerId);
    }

    public class AmsMusicLayout(IAudioRepository audioRepository) : IAmsMusicLayout
    {
        public const string KeyStateGroupName = "WH3_Campaign_Music_AMS_Key";
        public const string CallResponseStateGroupName = "WH3_Campaign_Music_AMS_Fragments_CallResponse";
        public const string Call = "Call";
        public const string Response = "Response";

        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly Dictionary<string, IReadOnlyList<AmsPulseSlot>> _pulseSlotsByStateGroup = [];
        private readonly Dictionary<uint, IReadOnlyList<string>> _fragmentKeysByContainer = [];

        /// <summary>C_01 is C; None, or anything without a key name, is no key.</summary>
        public static string GetKey(string keyStateName)
        {
            if (string.IsNullOrEmpty(keyStateName) || string.Equals(keyStateName, "None", StringComparison.OrdinalIgnoreCase))
                return null;

            var separator = keyStateName.IndexOf('_');
            return separator <= 0 ? keyStateName : keyStateName[..separator];
        }

        public IReadOnlyList<AmsPulseSlot> GetPulseSlots(string stateGroupName)
        {
            if (_pulseSlotsByStateGroup.TryGetValue(stateGroupName, out var cached))
                return cached;

            var slots = new List<AmsPulseSlot>();
            var keysBySequenceId = new Dictionary<uint, string>();

            foreach (var track in _audioRepository.GetVanillaAmsPulseTracks(stateGroupName).DistinctBy(track => track.Id))
            {
                if (GetVanillaHirc(track.NodeBaseParams.DirectParentId) is not CAkMusicSegment_V136 segment)
                    continue;

                var sequenceId = segment.MusicNodeParams.NodeBaseParams.DirectParentId;
                if (GetVanillaHirc(sequenceId) is not CAkMusicRanSeqCntr_V136 sequence)
                    continue;

                if (!keysBySequenceId.TryGetValue(sequenceId, out var key))
                    keysBySequenceId[sequenceId] = key = GetKeyOfPulseSequence(sequence);

                var playlist = sequence.PlayList.FirstOrDefault()?.PlayList.Select(item => item.SegmentId).ToList() ?? [];
                var variation = playlist.IndexOf(segment.Id) + 1;
                slots.Add(new AmsPulseSlot(track, key, variation));
            }

            var ordered = slots
                .OrderBy(slot => slot.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(slot => slot.Variation)
                .ToList();
            _pulseSlotsByStateGroup[stateGroupName] = ordered;
            return ordered;
        }

        /// <summary>The key a pulse playlist plays in: the values of the key container above it that
        /// lead to it, which are always one key's three.</summary>
        private string GetKeyOfPulseSequence(CAkMusicRanSeqCntr_V136 sequence)
        {
            var parentId = sequence.MusicTransNodeParams.MusicNodeParams.NodeBaseParams.DirectParentId;
            if (GetVanillaHirc(parentId) is not CAkMusicSwitchCntr_V136 keyContainer)
                return null;

            return new StatePathParser(_audioRepository).GetStatePaths(keyContainer).StatePaths
                .Where(statePath => statePath.ChildNodeId == sequence.Id)
                .Select(statePath => GetKey(statePath.Items.Last().DisplayName))
                .FirstOrDefault(key => key != null);
        }

        public IReadOnlyList<string> GetFragmentKeys(uint fragmentContainerId)
        {
            if (_fragmentKeysByContainer.TryGetValue(fragmentContainerId, out var cached))
                return cached;

            var keys = new List<string>();
            if (GetVanillaHirc(fragmentContainerId) is CAkSwitchCntr_V136 factionSwitch)
            {
                // Any culture's key switch lists the keys; the one with the most is the safe pick.
                var keySwitch = factionSwitch.Children.ChildIds
                    .Select(GetVanillaHirc)
                    .OfType<CAkSwitchCntr_V136>()
                    .OrderByDescending(container => container.SwitchList.Count)
                    .FirstOrDefault();

                if (keySwitch != null)
                {
                    keys.AddRange(keySwitch.SwitchList
                        .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                        .Select(package => GetKey(_audioRepository.GetNameFromId(package.SwitchId)))
                        .Where(key => key != null)
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                }
            }

            _fragmentKeysByContainer[fragmentContainerId] = keys;
            return keys;
        }

        private HircItem GetVanillaHirc(uint id) => _audioRepository.GetHircs(id).FirstOrDefault(hirc => hirc.IsCA);
    }
}
