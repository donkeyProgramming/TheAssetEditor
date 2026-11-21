using System;
using System.Linq;
using Editors.Shared.Core.Common.ReferenceModel;
using Shared.GameFormats.AnimationPack;

namespace AnimationEditor.MountAnimationCreator.ViewModels
{
    /// <summary>The rider and mount animation sets and slots, as picked in the Rider and Mount panels. Picking a
    /// mount slot picks the rider slot of the same name (RIDER_ and the mount slot).</summary>
    public class RiderAndMountSets
    {
        public BinAnimationViewModel Rider { get; }
        public BinAnimationViewModel Mount { get; }

        public IAnimationBinGenericFormat RiderSet => Rider.FragmentList.SelectedItem;
        public IAnimationBinGenericFormat MountSet => Mount.FragmentList.SelectedItem;

        public RiderAndMountSets(BinAnimationViewModel rider, BinAnimationViewModel mount, Action changed)
        {
            Rider = rider;
            Mount = mount;

            Mount.FragmentSlotList.SelectedItemChanged += OnMountSlotPicked;
            Rider.FragmentList.SelectedItemChanged += _ => changed();
            Mount.FragmentList.SelectedItemChanged += _ => changed();
        }

        void OnMountSlotPicked(AnimationBinEntryGenericFormat slot)
        {
            if (slot == null || RiderSet == null)
                return;
            Rider.FragmentSlotList.Filter = "";
            Rider.FragmentSlotList.SelectedItem = Rider.FragmentSlotList.Values.FirstOrDefault(x => x.SlotName == "RIDER_" + slot.SlotName);
        }

        /// <summary>Reloads both set lists, keeping the picked sets and slots.</summary>
        public void Reload()
        {
            // Taken first: re-picking the mount slot also picks its RIDER_ slot, over the one the user picked
            var riderSlot = Rider.FragmentSlotList.SelectedItem;
            Mount.ReloadFragments(Mount.FragmentSlotList.SelectedItem);
            Rider.ReloadFragments(riderSlot);
        }
    }
}
