using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.Shared.Wwise.Generators.Hirc.V136
{
    /// <summary>
    /// Builds the Music Random Sequence a decision tree branch points at, matching the vanilla
    /// subculture branches in the shipped banks: parented to the Music Switch container
    /// the branch is merged into, one playlist root over the segments, and one transition rule.
    /// </summary>
    public class CAkMusicRanSeqCntrGenerator_V136 : IHircGeneratorService
    {
        public HircItem GenerateHirc(AudioProjectItem audioProjectItem, SoundBank soundBank = null)
        {
            var randomSequence = audioProjectItem as MusicRandomSequence;

            var ranSeqHirc = new CAkMusicRanSeqCntr_V136
            {
                Id = randomSequence.Id,
                HircType = randomSequence.HircType
            };

            var transNodeParams = ranSeqHirc.MusicTransNodeParams;
            var nodeParams = transNodeParams.MusicNodeParams;

            // The container hangs off the vanilla switch container whose tree names it, and lists
            // its segments as children as well as in the playlist below.
            nodeParams.NodeBaseParams.DirectParentId = randomSequence.DirectParentId;
            nodeParams.NodeBaseParams.OverrideBusId = randomSequence.OverrideBusId;
            nodeParams.AkMeterInfo.Tempo = randomSequence.Tempo;
            nodeParams.AkMeterInfo.TimeSigNumBeatsBar = 4;
            nodeParams.AkMeterInfo.TimeSigBeatValue = 4;
            nodeParams.AkMeterInfo.GridPeriod = Wh3MusicHierarchyInformation.DefaultGridPeriod;

            foreach (var segment in randomSequence.Segments)
                nodeParams.Children.ChildIds.Add(segment.SegmentId);

            transNodeParams.PlayList.Add(CreateTransitionRule());
            ranSeqHirc.PlayList.Add(CreatePlaylistRoot(randomSequence));

            ranSeqHirc.UpdateSectionSize();
            return ranSeqHirc;
        }

        private static CAkMusicRanSeqCntr_V136.AkMusicRanSeqPlaylistItem_V136 CreatePlaylistRoot(MusicRandomSequence randomSequence)
        {
            var isSingleSegment = randomSequence.Segments.Count == 1;

            // A lone segment is a continuous sequence looping forever (loop 0); several segments are
            // a weighted random pick that avoids playing the same one twice running. Both are
            // straight out of vanilla - the first from the tree's default branch, the second from
            // the cultures that ship music variations.
            var root = new CAkMusicRanSeqCntr_V136.AkMusicRanSeqPlaylistItem_V136
            {
                SegmentId = 0,
                PlaylistItemId = randomSequence.PlaylistRootItemId,
                RsType = isSingleSegment
                    ? Wh3MusicHierarchyInformation.PlaylistTypeSequenceContinuous
                    : Wh3MusicHierarchyInformation.PlaylistTypeRandomStep,
                Loop = 0,
                Weight = Wh3MusicHierarchyInformation.PlaylistDefaultWeight,
                AvoidRepeatCount = 1,
                IsUsingWeight = 1,
                IsShuffle = 0
            };

            foreach (var segment in randomSequence.Segments)
            {
                root.PlayList.Add(new CAkMusicRanSeqCntr_V136.AkMusicRanSeqPlaylistItem_V136
                {
                    SegmentId = segment.SegmentId,
                    PlaylistItemId = segment.PlaylistItemId,
                    RsType = Wh3MusicHierarchyInformation.PlaylistTypeNone,

                    // The leaf plays through once and hands back to the root, which is what does the
                    // looping. A single segment is the exception: there is nothing to hand back to,
                    // so vanilla loops the leaf itself.
                    Loop = (short)(isSingleSegment ? 1 : 0),
                    Weight = Wh3MusicHierarchyInformation.PlaylistDefaultWeight,
                    AvoidRepeatCount = 0,
                    IsUsingWeight = 0,
                    IsShuffle = 0
                });
            }

            root.NumChildren = (uint)root.PlayList.Count;
            return root;
        }

        private static MusicTransNodeParams_V136.AkMusicTransitionRule_V136 CreateTransitionRule()
        {
            // Identical in every vanilla branch: anything to anything, no fade, cut at the exit cue.
            var rule = new MusicTransNodeParams_V136.AkMusicTransitionRule_V136();
            rule.SrcIdList.Add(Wh3MusicHierarchyInformation.AnyTransitionId);
            rule.DstIdList.Add(Wh3MusicHierarchyInformation.AnyTransitionId);

            rule.AkMusicTransSrcRule.TransitionTime = 0;
            rule.AkMusicTransSrcRule.FadeCurve = Wh3MusicHierarchyInformation.TransitionFadeCurve;
            rule.AkMusicTransSrcRule.FadeOffset = 0;
            rule.AkMusicTransSrcRule.SyncType = Wh3MusicHierarchyInformation.TransitionSyncTypeExitCue;
            rule.AkMusicTransSrcRule.CueFilterHash = 0;
            rule.AkMusicTransSrcRule.PlayPostExit = 1;

            rule.AkMusicTransDstRule.TransitionTime = 0;
            rule.AkMusicTransDstRule.FadeCurve = Wh3MusicHierarchyInformation.TransitionFadeCurve;
            rule.AkMusicTransDstRule.FadeOffset = 0;
            rule.AkMusicTransDstRule.CueFilterHash = 0;
            rule.AkMusicTransDstRule.JumpToId = 0;
            rule.AkMusicTransDstRule.JumpToType = 0;
            rule.AkMusicTransDstRule.EntryType = 0;
            rule.AkMusicTransDstRule.PlayPreEntry = 1;
            rule.AkMusicTransDstRule.DestMatchSourceCueName = 0;

            return rule;
        }
    }
}
