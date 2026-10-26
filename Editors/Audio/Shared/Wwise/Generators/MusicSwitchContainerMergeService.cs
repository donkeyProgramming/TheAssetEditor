using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;
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
    ///
    /// <paramref name="KeyPaths"/> are the full paths from the root that reach the branch, when a
    /// <see cref="MusicTree"/> has decided them - see <see cref="MusicSwitchContainerMergeService.BuildTreeKeyPaths"/>.
    /// Without them the branch is reached by every path below the State.
    /// </summary>
    public record MusicBranch(string StateGroupName, string StateName, uint RandomSequenceId, IReadOnlyList<List<uint>> KeyPaths = null);

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
                foreach (var keyPath in branch.KeyPaths ?? BuildKeyPaths(vanillaContainer, branch))
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
        /// The key to use at each level of the tree, as one path per combination.
        ///
        /// Above the level that branches on the State Group being set, the branch goes into every
        /// block where vanilla tells that Group's States apart - every block listing at least one of
        /// them by name. A block holding only the default is one where vanilla plays the same thing
        /// whatever the State, such as the per-battle tracks in the adaptive battle container, and
        /// the new State follows that the same as every vanilla one does. Battle result is lose, win
        /// or draw with every culture listed under all three, so the branch goes under all three.
        ///
        /// Below it, each level is filled in from what vanilla does there, because the branch has to
        /// be reachable whatever those States are at the time: the default where vanilla has one,
        /// which covers everything, or else every key vanilla uses.
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

            var keyPaths = PathsToBlocksListingStates(vanillaTree, branchLevel);
            if (keyPaths.Count == 0)
                throw new NotSupportedException(
                    $"Music Switch container {vanillaContainer.Id} never lists a State of '{branch.StateGroupName}' by name, " +
                    "so there is no block a new one belongs in.");

            for (var level = branchLevel; level < vanillaContainer.TreeDepth; level++)
            {
                List<uint> keysForLevel = level == branchLevel
                    ? [WwiseHash.Compute(branch.StateName)]
                    : GetVanillaKeysAtLevel(vanillaTree, level);

                keyPaths = [.. keyPaths.SelectMany(keyPath => keysForLevel.Select(key => new List<uint>(keyPath) { key }))];
            }

            return keyPaths;
        }

        /// <summary>
        /// Every full decision tree path, per node of the tree, that reaches the node's audio in this
        /// container. A node that is never reached here - its audio is for the other container, or
        /// every combination it covers is claimed by a branch below it - has no entry.
        ///
        /// The modder's tree can split on the levels in any order and can leave any level out, but
        /// Wwise reads a container's levels in a fixed order, preferring an exact key to the default
        /// at each one. A path per tree branch would not be read the same way: a split on the battle
        /// situation above a split on the key would lose to it, since the key comes first. So this
        /// writes out every combination of the values the tree names, plus the default, and asks
        /// the modder's tree which node each one reaches. With every combination present Wwise always
        /// finds the exact one, and it leads where the modder's tree would.
        ///
        /// The levels above the culture are not chosen here: the culture's branch goes in every block
        /// vanilla lists cultures in, the same as without a tree, and the tree is asked with that
        /// block's values. Below the culture, a level the tree never names takes the default where
        /// vanilla has one and every vanilla key where it has not, again the same as without one.
        /// </summary>
        public static Dictionary<int, List<List<uint>>> BuildTreeKeyPaths(CAkMusicSwitchCntr_V136 vanillaContainer, MusicTree tree)
        {
            ArgumentNullException.ThrowIfNull(vanillaContainer);
            ArgumentNullException.ThrowIfNull(tree);

            var levelGroupIds = vanillaContainer.Arguments.Select(argument => argument.GroupId).ToList();
            var cultureLevel = levelGroupIds.IndexOf(WwiseHash.Compute(tree.StateGroupName));
            if (cultureLevel == -1)
                throw new NotSupportedException(
                    $"Music Switch container {vanillaContainer.Id} does not branch on State Group '{tree.StateGroupName}'.");

            var vanillaTree = vanillaContainer.AkDecisionTree.DecisionTree;
            var prefixes = PathsToBlocksListingStates(vanillaTree, cultureLevel);
            if (prefixes.Count == 0)
                throw new NotSupportedException(
                    $"Music Switch container {vanillaContainer.Id} never lists a State of '{tree.StateGroupName}' by name, " +
                    "so there is no block a new one belongs in.");

            var namedKeysByGroupId = tree.GetNodes()
                .Where(node => node.SplitBy != null && node.SplitBy != Wh3MusicTreeLevelInformation.ContainerLevel)
                .GroupBy(node => WwiseHash.Compute(node.SplitBy))
                .ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(node => node.Branches).Where(branch => branch.Value != null).Select(branch => WwiseHash.Compute(branch.Value)).ToList());

            var keysBelowCulture = new List<List<uint>>();
            for (var level = cultureLevel + 1; level < vanillaContainer.TreeDepth; level++)
            {
                var keys = GetVanillaKeysAtLevel(vanillaTree, level);
                if (namedKeysByGroupId.TryGetValue(levelGroupIds[level], out var namedKeys))
                    keys = [.. keys.Union(namedKeys)];
                keysBelowCulture.Add(keys);
            }

            var stateKey = WwiseHash.Compute(tree.StateName);
            var pathsByNodeId = new Dictionary<int, List<List<uint>>>();

            foreach (var prefix in prefixes)
            {
                foreach (var keysBelow in CartesianProduct(keysBelowCulture))
                {
                    List<uint> keyPath = [.. prefix, stateKey, .. keysBelow];

                    uint? KeyFor(uint groupId)
                    {
                        var level = levelGroupIds.IndexOf(groupId);
                        return level == -1 ? null : keyPath[level];
                    }

                    var node = Evaluate(tree.Root, KeyFor, vanillaContainer.Id);
                    if (node == null)
                        continue;

                    if (!pathsByNodeId.TryGetValue(node.Id, out var paths))
                        pathsByNodeId[node.Id] = paths = [];
                    paths.Add(keyPath);
                }
            }

            return pathsByNodeId;
        }

        /// <summary>
        /// The node a modder's tree leads to for one combination: down through the branches matching
        /// it, falling back to the "any other value" branch, and stopping at a split on a level this
        /// container does not have. The deepest node on the way that has audio is the one that plays;
        /// null when none has.
        /// </summary>
        public static MusicTreeNode Evaluate(MusicTreeNode root, Func<uint, uint?> keyForGroupId, uint containerId)
        {
            var selected = root.AudioFileIds.Count != 0 ? root : null;
            var node = root;

            while (node.SplitBy != null)
            {
                MusicTreeNode next;
                if (node.SplitBy == Wh3MusicTreeLevelInformation.ContainerLevel)
                {
                    next = node.Branches.FirstOrDefault(branch => branch.Value == containerId.ToString());
                }
                else
                {
                    if (keyForGroupId(WwiseHash.Compute(node.SplitBy)) is not uint key)
                        break;

                    next = key == DefaultKey ? null : node.Branches.FirstOrDefault(branch => branch.Value != null && WwiseHash.Compute(branch.Value) == key);
                }

                next ??= node.Branches.FirstOrDefault(branch => branch.Value == null);
                if (next == null)
                    break;

                node = next;
                if (node.AudioFileIds.Count != 0)
                    selected = node;
            }

            return selected;
        }

        private static IEnumerable<List<uint>> CartesianProduct(List<List<uint>> keysPerLevel)
        {
            IEnumerable<List<uint>> combinations = [[]];
            foreach (var keys in keysPerLevel)
                combinations = combinations.SelectMany(combination => keys.Select(key => new List<uint>(combination) { key }));
            return combinations;
        }

        /// <summary>The key paths down to every block at <paramref name="level"/> that names at least
        /// one key other than the default.</summary>
        private static List<List<uint>> PathsToBlocksListingStates(AkDecisionTree_V136.Node_V136 vanillaTree, int level)
        {
            var paths = new List<List<uint>>();

            void Walk(AkDecisionTree_V136.Node_V136 node, int depth, List<uint> path)
            {
                if (depth == level)
                {
                    if (node.Nodes.Any(child => child.Key != DefaultKey))
                        paths.Add(path);
                    return;
                }

                foreach (var child in node.Nodes)
                    Walk(child, depth + 1, [.. path, child.Key]);
            }

            Walk(vanillaTree, 0, []);
            return paths;
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
        private static void InsertBranch(AkDecisionTree_V136.Node_V136 moddedRoot, IReadOnlyList<uint> keyPath, uint randomSequenceId)
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
