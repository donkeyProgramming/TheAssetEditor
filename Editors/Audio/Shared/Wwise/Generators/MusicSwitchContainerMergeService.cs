using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.Shared.Wwise.Generators
{
    /// <summary>
    /// One branch to add to a vanilla Music Switch container's decision tree. The State Group is
    /// carried alongside the State because a container can branch on more than one Group, and the
    /// merge has to know which level of the tree this State belongs at.
    /// </summary>
    public record MusicBranch(string StateGroupName, string StateName, uint RandomSequenceId);

    public interface IMusicSwitchContainerMergeService
    {
        CAkMusicSwitchCntr_V136 CreateModdedContainer(CAkMusicSwitchCntr_V136 vanillaContainer, IReadOnlyList<MusicBranch> branches);
        CAkMusicSwitchCntr_V136 MergeBranches(CAkMusicSwitchCntr_V136 vanillaContainer, IReadOnlyList<MusicBranch> branches);
        CAkMusicSwitchCntr_V136 MergeContainers(CAkMusicSwitchCntr_V136 baseContainer, CAkMusicSwitchCntr_V136 mergingContainer);
    }

    /// <summary>
    /// Re-emits a vanilla Music Switch container carrying a mod's branches, the same way
    /// <see cref="SoundBankGeneratorService"/> re-emits a whole Dialogue Event rather than trying to
    /// patch one in place.
    ///
    /// A music Action Event only sets a State. Nothing plays until that State selects a branch here,
    /// so this is what turns a generated music hierarchy into audio the game will actually reach.
    ///
    /// Two forms, because the Dialogue Event path needs both and music needs them for the same
    /// reasons: <see cref="CreateModdedContainer"/> carries the mod's branches alone and goes in the
    /// merging .bnk, so several mods can be combined before vanilla is folded in;
    /// <see cref="MergeBranches"/> folds vanilla in immediately and goes in the testing .bnk, so one
    /// mod can be played on its own.
    /// </summary>
    public class MusicSwitchContainerMergeService : IMusicSwitchContainerMergeService
    {
        public CAkMusicSwitchCntr_V136 CreateModdedContainer(CAkMusicSwitchCntr_V136 vanillaContainer, IReadOnlyList<MusicBranch> branches)
        {
            ArgumentNullException.ThrowIfNull(vanillaContainer);
            ArgumentNullException.ThrowIfNull(branches);

            var moddedRoot = new AkDecisionTree_V136.Node_V136();

            foreach (var branch in branches)
            {
                foreach (var keyPath in BuildKeyPaths(vanillaContainer, branch))
                    InsertBranch(moddedRoot, keyPath, branch.RandomSequenceId);
            }

            return CopyWithDecisionTree(vanillaContainer, moddedRoot);
        }

        public CAkMusicSwitchCntr_V136 MergeBranches(CAkMusicSwitchCntr_V136 vanillaContainer, IReadOnlyList<MusicBranch> branches)
        {
            var moddedContainer = CreateModdedContainer(vanillaContainer, branches);

            // The modded tree is the base so that a branch for a State vanilla already covers
            // replaces it, which is what a modder replacing a culture's music is asking for.
            var mergedTree = AkDecisionTree_V136.MergeDecisionTrees(
                moddedContainer.AkDecisionTree.DecisionTree,
                vanillaContainer.AkDecisionTree.DecisionTree);

            return CopyWithDecisionTree(vanillaContainer, mergedTree);
        }

        /// <summary>
        /// Folds one container's branches into another's, for the merger combining several mods.
        /// The base takes priority where both claim the same State, and everything outside the tree
        /// comes from the base. The merger starts from vanilla, so vanilla is what wins - the same
        /// tie break the Dialogue Event merge uses. Branches normally add a subculture vanilla does
        /// not have, so nothing collides and the order does not come up.
        /// </summary>
        public CAkMusicSwitchCntr_V136 MergeContainers(CAkMusicSwitchCntr_V136 baseContainer, CAkMusicSwitchCntr_V136 mergingContainer)
        {
            ArgumentNullException.ThrowIfNull(baseContainer);
            ArgumentNullException.ThrowIfNull(mergingContainer);

            var mergedTree = AkDecisionTree_V136.MergeDecisionTrees(
                baseContainer.AkDecisionTree.DecisionTree,
                mergingContainer.AkDecisionTree.DecisionTree);

            return CopyWithDecisionTree(baseContainer, mergedTree);
        }

        /// <summary>
        /// A new container rather than the one passed in. The vanilla hirc belongs to the audio
        /// repository and is shared with everything else reading vanilla data, so this must not edit
        /// it in place. Everything outside the decision tree and the child list is carried over
        /// unchanged - the arguments, the mode and the transition parameters are what make the
        /// container the one the game is already asking for.
        /// </summary>
        private static CAkMusicSwitchCntr_V136 CopyWithDecisionTree(CAkMusicSwitchCntr_V136 vanillaContainer, AkDecisionTree_V136.Node_V136 decisionTree)
        {
            var container = new CAkMusicSwitchCntr_V136
            {
                Id = vanillaContainer.Id,
                HircType = vanillaContainer.HircType,
                MusicTransNodeParams = CopyWithChildren(vanillaContainer.MusicTransNodeParams,
                    vanillaContainer.AkDecisionTree.DecisionTree, decisionTree),
                IsContinuePlayback = vanillaContainer.IsContinuePlayback,
                TreeDepth = vanillaContainer.TreeDepth,
                Arguments = [.. vanillaContainer.Arguments],
                Mode = vanillaContainer.Mode,
                AkDecisionTree = new AkDecisionTree_V136
                {
                    DecisionTree = decisionTree,
                    Nodes = AkDecisionTree_V136.FlattenDecisionTree(decisionTree)
                }
            };

            container.TreeDataSize = container.AkDecisionTree.GetSize();
            container.UpdateSectionSize();
            return container;
        }

        /// <summary>
        /// The transition parameters with a child list that covers the merged tree. A decision tree
        /// leaf is only resolved if the container also claims that node as a child, so adding a
        /// branch and leaving the child list alone gives a container that silently falls through to
        /// its default - which for a new culture means vanilla music keeps playing. Vanilla lists
        /// its children in ascending id order and this keeps to that.
        ///
        /// Only leaves the merge introduced are added - ones the base container's own tree did not
        /// already point at. Vanilla trees also point at nodes they do not claim: four of the six
        /// containers in campaign_music__core.bnk name up to 349 nodes missing from their child
        /// lists, presumably claimed by another container. Claiming those as well made a container
        /// merged with nothing but itself come back larger, and handed those nodes a second parent.
        ///
        /// Everything but the child list is shared with the vanilla hirc rather than copied, which
        /// is safe only because nothing here writes to it.
        /// </summary>
        private static MusicTransNodeParams_V136 CopyWithChildren(MusicTransNodeParams_V136 vanillaParams,
            AkDecisionTree_V136.Node_V136 vanillaTree, AkDecisionTree_V136.Node_V136 decisionTree)
        {
            var childIds = new SortedSet<uint>(vanillaParams.MusicNodeParams.Children.ChildIds);
            var alreadyReferenced = new HashSet<uint>(CollectAudioNodeIds(vanillaTree));
            foreach (var audioNodeId in CollectAudioNodeIds(decisionTree))
            {
                // The default at a level is stored as a key of zero with no node behind it.
                if (audioNodeId != 0 && !alreadyReferenced.Contains(audioNodeId))
                    childIds.Add(audioNodeId);
            }

            var vanillaNodeParams = vanillaParams.MusicNodeParams;
            return new MusicTransNodeParams_V136
            {
                NumRules = vanillaParams.NumRules,
                PlayList = vanillaParams.PlayList,
                MusicNodeParams = new MusicNodeParams_V136
                {
                    Flags = vanillaNodeParams.Flags,
                    NodeBaseParams = vanillaNodeParams.NodeBaseParams,
                    AkMeterInfo = vanillaNodeParams.AkMeterInfo,
                    MeterInfoFlag = vanillaNodeParams.MeterInfoFlag,
                    NumStingers = vanillaNodeParams.NumStingers,
                    StingersList = vanillaNodeParams.StingersList,
                    Children = new Children_V136
                    {
                        NumChilds = (uint)childIds.Count,
                        ChildIds = [.. childIds]
                    }
                }
            };
        }

        private static IEnumerable<uint> CollectAudioNodeIds(AkDecisionTree_V136.Node_V136 node)
        {
            foreach (var child in node.Nodes)
            {
                if (child.Nodes.Count == 0)
                    yield return child.AudioNodeId;
                else
                    foreach (var audioNodeId in CollectAudioNodeIds(child))
                        yield return audioNodeId;
            }
        }

        /// <summary>
        /// The key to use at each level of the tree, as one path per combination. The level that
        /// branches on the State Group being set gets the State; every other level is filled in from
        /// what vanilla already does there, because the branch has to be reachable no matter what
        /// those other States happen to be at the time.
        ///
        /// Where vanilla has a default at a level, that one key covers everything and is the whole
        /// answer. Where it does not - battle result is lose, win or draw with no default - the
        /// branch is repeated once per key instead, which is how vanilla itself lists every culture
        /// under all three results.
        /// </summary>
        private static List<List<uint>> BuildKeyPaths(CAkMusicSwitchCntr_V136 vanillaContainer, MusicBranch branch)
        {
            var stateGroupId = WwiseHash.Compute(branch.StateGroupName);
            var vanillaTree = vanillaContainer.AkDecisionTree.DecisionTree;

            var branchLevel = vanillaContainer.Arguments
                .FindIndex(argument => argument.GroupId == stateGroupId);

            if (branchLevel == -1)
                throw new NotSupportedException(
                    $"Music Switch container {vanillaContainer.Id} does not branch on State Group " +
                    $"'{branch.StateGroupName}', so a State from it selects nothing there.");

            var keyPaths = new List<List<uint>> { new() };

            for (var level = 0; level < vanillaContainer.TreeDepth; level++)
            {
                List<uint> keysForLevel = level == branchLevel
                    ? [WwiseHash.Compute(branch.StateName)]
                    : GetVanillaKeysAtLevel(vanillaTree, level);

                keyPaths = [.. keyPaths.SelectMany(keyPath => keysForLevel.Select(key => new List<uint>(keyPath) { key }))];
            }

            return keyPaths;
        }

        /// <summary>
        /// The keys vanilla uses at one level of the tree, or just the default when it has one.
        /// An empty level yields the default, so a container with a level nothing has been written
        /// against still produces a reachable path rather than none at all.
        /// </summary>
        private static List<uint> GetVanillaKeysAtLevel(AkDecisionTree_V136.Node_V136 vanillaTree, int level)
        {
            var nodesAtLevel = new List<AkDecisionTree_V136.Node_V136> { vanillaTree };
            for (var depth = 0; depth < level; depth++)
                nodesAtLevel = [.. nodesAtLevel.SelectMany(node => node.Nodes)];

            var keys = nodesAtLevel
                .SelectMany(node => node.Nodes)
                .Select(node => node.Key)
                .Distinct()
                .ToList();

            if (keys.Count == 0 || keys.Contains(DefaultKey))
                return [DefaultKey];

            return keys;
        }

        /// <summary>
        /// Walks a path into the modded tree, reusing the nodes already on it. Paths for the same
        /// branch share every level above the one that differs, which is what keeps three battle
        /// results from producing three separate copies of the levels above them.
        /// </summary>
        private static void InsertBranch(AkDecisionTree_V136.Node_V136 moddedRoot, List<uint> keyPath, uint randomSequenceId)
        {
            var node = moddedRoot;

            for (var level = 0; level < keyPath.Count; level++)
            {
                var key = keyPath[level];
                var child = node.Nodes.FirstOrDefault(existing => existing.Key == key);

                if (child == null)
                {
                    child = new AkDecisionTree_V136.Node_V136
                    {
                        Key = key,
                        Weight = Wh3MusicHierarchyInformation.BranchWeight,
                        Probability = Wh3MusicHierarchyInformation.BranchProbability
                    };
                    node.Nodes.Add(child);
                }

                node = child;
            }

            // Only the leaf names audio. An intermediate node carrying one would be read as a leaf
            // and everything below it would become unreachable.
            node.AudioNodeId = randomSequenceId;
        }

        /// <summary>The key Wwise reads as 'any value of this State Group'.</summary>
        private const uint DefaultKey = 0;
    }
}
