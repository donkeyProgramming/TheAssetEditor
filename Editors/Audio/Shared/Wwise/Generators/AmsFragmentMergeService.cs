using System;
using System.Collections.Generic;
using System.Linq;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Editors.Audio.Shared.Wwise.Generators
{
    /// <summary>One culture to add to the vanilla ambient fragments container.</summary>
    public record AmsFragmentBranch(string StateName, uint KeySwitchContainerId);

    public interface IAmsFragmentMergeService
    {
        CAkSwitchCntr_V136 AddCultures(CAkSwitchCntr_V136 vanillaContainer, IReadOnlyList<AmsFragmentBranch> branches);
        CAkSwitchCntr_V136 MergeContainers(CAkSwitchCntr_V136 baseContainer, CAkSwitchCntr_V136 mergingContainer);
        CAkSwitchCntr_V136 CreateKeySwitchContainer(
            CAkSwitchCntr_V136 vanillaContainer, IAudioRepositoryLookup lookup, uint keySwitchContainerId, Func<uint, uint> targetForKeySwitchId);
        CAkSwitchCntr_V136 CreateCallResponseSwitchContainer(
            CAkSwitchCntr_V136 vanillaContainer, IAudioRepositoryLookup lookup, uint id, uint parentId, uint callTargetId, uint responseTargetId);
    }

    /// <summary>Just enough of the repository for the key switch to be mirrored from a sibling,
    /// rather than taking the whole interface for one lookup.</summary>
    public interface IAudioRepositoryLookup
    {
        CAkSwitchCntr_V136 FindSwitchContainer(uint id);
    }

    /// <summary>
    /// Adds a culture to the vanilla ambient fragments Switch container, and builds the key switch
    /// that culture's audio hangs off.
    ///
    /// Unlike a switch track, a Switch container identifies its branches by switch id rather than by
    /// position, so this merges by key the way a decision tree does and two mods cannot collide by
    /// accident. The children list still has to be kept in step with the switch list - a switch
    /// pointing at a node the container does not claim as a child is not resolved.
    /// </summary>
    public class AmsFragmentMergeService : IAmsFragmentMergeService
    {
        public CAkSwitchCntr_V136 AddCultures(CAkSwitchCntr_V136 vanillaContainer, IReadOnlyList<AmsFragmentBranch> branches)
        {
            ArgumentNullException.ThrowIfNull(vanillaContainer);
            ArgumentNullException.ThrowIfNull(branches);

            var merged = CopyContainer(vanillaContainer);

            foreach (var branch in branches)
            {
                var switchId = WwiseHash.Compute(branch.StateName);

                var existing = merged.SwitchList
                    .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                    .FirstOrDefault(switchPackage => switchPackage.SwitchId == switchId);

                if (existing != null)
                {
                    // Re-pointed rather than added twice; the first package would win and the second
                    // would never be looked at.
                    existing.NodeIdList.Clear();
                    existing.NodeIdList.Add(branch.KeySwitchContainerId);
                }
                else
                {
                    merged.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136
                    {
                        SwitchId = switchId,
                        NodeIdList = [branch.KeySwitchContainerId]
                    });
                }

                if (!merged.Children.ChildIds.Contains(branch.KeySwitchContainerId))
                    merged.Children.ChildIds.Add(branch.KeySwitchContainerId);
            }

            merged.NumSwitchGroups = (uint)merged.SwitchList.Count;
            merged.Children.NumChilds = (uint)merged.Children.ChildIds.Count;
            merged.UpdateSectionSize();
            return merged;
        }

        /// <summary>
        /// Folds one mod's container into another's. The base takes priority where two claim the same
        /// culture, the same tie break every other merge here uses.
        /// </summary>
        public CAkSwitchCntr_V136 MergeContainers(CAkSwitchCntr_V136 baseContainer, CAkSwitchCntr_V136 mergingContainer)
        {
            ArgumentNullException.ThrowIfNull(baseContainer);
            ArgumentNullException.ThrowIfNull(mergingContainer);

            var merged = CopyContainer(baseContainer);
            var claimedSwitchIds = merged.SwitchList
                .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                .Select(switchPackage => switchPackage.SwitchId)
                .ToHashSet();

            foreach (var switchPackage in mergingContainer.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>())
            {
                if (claimedSwitchIds.Contains(switchPackage.SwitchId))
                    continue;

                merged.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136
                {
                    SwitchId = switchPackage.SwitchId,
                    NodeIdList = [.. switchPackage.NodeIdList]
                });

                foreach (var nodeId in switchPackage.NodeIdList)
                {
                    if (!merged.Children.ChildIds.Contains(nodeId))
                        merged.Children.ChildIds.Add(nodeId);
                }
            }

            merged.NumSwitchGroups = (uint)merged.SwitchList.Count;
            merged.Children.NumChilds = (uint)merged.Children.ChildIds.Count;
            merged.UpdateSectionSize();
            return merged;
        }

        /// <summary>
        /// The Switch container on the musical key Group that a culture's fragments hang off, with the
        /// keys an existing culture's key switch lists, each pointing where
        /// <paramref name="targetForKeySwitchId"/> says. A key it gives 0 is left out, so it plays
        /// nothing the way vanilla's silent "None" branch does; the default is kept as vanilla's.
        /// </summary>
        public CAkSwitchCntr_V136 CreateKeySwitchContainer(
            CAkSwitchCntr_V136 vanillaContainer, IAudioRepositoryLookup lookup, uint keySwitchContainerId, Func<uint, uint> targetForKeySwitchId)
        {
            var sibling = FindKeySwitch(vanillaContainer, lookup);

            var keySwitch = CreateSwitchLike(sibling, keySwitchContainerId, vanillaContainer.Id);
            foreach (var siblingSwitch in sibling.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>())
            {
                var target = targetForKeySwitchId(siblingSwitch.SwitchId);
                if (target != 0)
                    keySwitch.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136 { SwitchId = siblingSwitch.SwitchId, NodeIdList = [target] });
            }

            return Finish(keySwitch);
        }

        /// <summary>
        /// The Switch on Call or Response under a key, mirrored from the one vanilla puts under an
        /// existing culture's keys, with Call and Response each pointing at what the tree plays there.
        /// A 0 target is left out, and plays nothing.
        /// </summary>
        public CAkSwitchCntr_V136 CreateCallResponseSwitchContainer(
            CAkSwitchCntr_V136 vanillaContainer, IAudioRepositoryLookup lookup, uint id, uint parentId, uint callTargetId, uint responseTargetId)
        {
            var sibling = FindKeySwitch(vanillaContainer, lookup).SwitchList
                .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                .SelectMany(package => package.NodeIdList)
                .Select(lookup.FindSwitchContainer)
                .FirstOrDefault(child => child != null && child.SwitchList.Count != 0)
                ?? throw new NotSupportedException(
                    $"No culture under Switch container {vanillaContainer.Id} splits its fragments by call and response, so there is none to mirror.");

            var callResponse = CreateSwitchLike(sibling, id, parentId);
            foreach (var (name, target) in new[] { (AudioProject.AmsMusicLayout.Call, callTargetId), (AudioProject.AmsMusicLayout.Response, responseTargetId) })
            {
                if (target != 0)
                    callResponse.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136 { SwitchId = WwiseHash.Compute(name), NodeIdList = [target] });
            }

            return Finish(callResponse);
        }

        private static CAkSwitchCntr_V136 FindKeySwitch(CAkSwitchCntr_V136 vanillaContainer, IAudioRepositoryLookup lookup) =>
            vanillaContainer.Children.ChildIds
                .Select(lookup.FindSwitchContainer)
                .Where(child => child != null && child.SwitchList.Count != 0)
                .OrderByDescending(child => child.SwitchList.Count)
                .FirstOrDefault()
            ?? throw new NotSupportedException(
                $"Switch container {vanillaContainer.Id} has no child Switch container to take the musical keys from, " +
                "so a new culture's fragments cannot be keyed the way vanilla's are.");

        private static CAkSwitchCntr_V136 CreateSwitchLike(CAkSwitchCntr_V136 sibling, uint id, uint parentId) => new()
        {
            Id = id,
            HircType = AkBkHircType.SwitchContainer,
            EGroupType = sibling.EGroupType,
            GroupId = sibling.GroupId,
            DefaultSwitch = sibling.DefaultSwitch,
            BIsContinuousValidation = sibling.BIsContinuousValidation,
            NodeBaseParams = new NodeBaseParams_V136 { DirectParentId = parentId }
        };

        /// <summary>The children are the distinct nodes the switches name, as Wwise requires.</summary>
        private static CAkSwitchCntr_V136 Finish(CAkSwitchCntr_V136 container)
        {
            container.Children.ChildIds.AddRange(container.SwitchList
                .Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>()
                .SelectMany(package => package.NodeIdList)
                .Distinct()
                .Order());
            container.Children.NumChilds = (uint)container.Children.ChildIds.Count;
            container.NumSwitchGroups = (uint)container.SwitchList.Count;
            container.UpdateSectionSize();
            return container;
        }

        private static CAkSwitchCntr_V136 CopyContainer(CAkSwitchCntr_V136 source)
        {
            var copy = new CAkSwitchCntr_V136
            {
                Id = source.Id,
                HircType = source.HircType,
                NodeBaseParams = source.NodeBaseParams,
                EGroupType = source.EGroupType,
                GroupId = source.GroupId,
                DefaultSwitch = source.DefaultSwitch,
                BIsContinuousValidation = source.BIsContinuousValidation,
                NumSwitchParams = source.NumSwitchParams
            };

            copy.Children.ChildIds.AddRange(source.Children.ChildIds);
            copy.Children.NumChilds = (uint)copy.Children.ChildIds.Count;
            copy.Parameters.AddRange(source.Parameters);

            // Rebuilt rather than shared so merging cannot edit the repository's copy.
            foreach (var switchPackage in source.SwitchList.Cast<CAkSwitchCntr_V136.CAkSwitchPackage_V136>())
            {
                copy.SwitchList.Add(new CAkSwitchCntr_V136.CAkSwitchPackage_V136
                {
                    SwitchId = switchPackage.SwitchId,
                    NodeIdList = [.. switchPackage.NodeIdList]
                });
            }

            copy.NumSwitchGroups = (uint)copy.SwitchList.Count;
            return copy;
        }
    }
}
