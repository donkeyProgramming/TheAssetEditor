using FileTypes.AnimationPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimationEditor.MountAnimationCreator.Services
{
    class BatchProcessorService
    {
        AnimationFragment _mountFragment;
        AnimationFragment _riderFragment;

        AnimationFragment _riderOutputFragment;
        AnimationPackFile _outputAnimPack;


        public BatchProcessorService(AnimationFragment mountFragment, AnimationFragment riderFragment)
        {
            _mountFragment = mountFragment;
            _riderFragment = riderFragment;

            var animPackName = "test_tables.bin";
            var animBinName = "hu1_test_hr1_hammer";
            var fragmentName = "test_fragment.frg";


            // Create a map matching rider and mount slots
            var animationMap = new Dictionary<string, string>();
            foreach (var item in _mountFragment.Fragments)
            {
                if (!animationMap.ContainsKey(item.Slot.Value))
                    animationMap.Add(item.Slot.Value, null);
            }

            // Find all the matching animations in the rider
            foreach (var riderItem in _riderFragment.Fragments)
            {
                var riderItemName = riderItem.Slot.Value;

                // Rider animations
                if (riderItemName.Contains("rider_", StringComparison.CurrentCultureIgnoreCase))
                {
                    var slotWithoutRiderPrefix = riderItemName.Remove(0, "rider_".Length);
                    if (animationMap.ContainsKey(slotWithoutRiderPrefix))
                    {
                        animationMap[slotWithoutRiderPrefix] = riderItemName;
                    }
                }

                // Porthole animations
                if (riderItemName.Contains("porthole_", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (animationMap.ContainsKey(riderItemName))
                        animationMap[riderItemName] = riderItemName;
                }

                if (riderItemName.Contains("missing_anim", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (animationMap.ContainsKey(riderItemName))
                        animationMap[riderItemName] = riderItemName;
                }
            }

            //AnimationPackLoader
            AnimationPackFile animPack = new AnimationPackFile();
            animPack.AnimationBin = new AnimationBin("animations//animation_tables//" + animPackName);
            var tableEntry = new AnimationBinEntry(animBinName, _riderFragment.Skeletons.Values.First(), _mountFragment.Skeletons.Values.First());
            tableEntry.FragmentReferences.Add(new AnimationBinEntry.FragmentReference() { Name = fragmentName });


            animPack.AnimationBin.AnimationTableEntries.Add(tableEntry);

            _riderOutputFragment = new AnimationFragment("animations//animation_tables//" + fragmentName);
            _riderOutputFragment.Skeletons = new AnimationFragment.StringArrayTable(_riderFragment.Skeletons.Values.First(), _riderFragment.Skeletons.Values.First());
            _debugMap = new Dictionary<string, string>();
            //     // Build the output animation file 
            CreateAnimation("MISSING_ANIM");
            
            foreach (var animationSlot in GetMetaDataSlots())
                CreateAnimation(animationSlot);

            foreach (var animationSlot in GetPoseAndDockAnimationsSlots())
                CreateAnimation(animationSlot);

            foreach (var animationSlot in GetPortholeSlots())
                CreateAnimation(animationSlot, animationSlot);

            foreach (var animationSlot in GetRiderMountAnimations())
                CreateAnimation(animationSlot.Item1, animationSlot.Item2);
            

            //foreach (var animationSlot in GetRiderAnimationsWithoutCasting())
            //    CreateAnimation(animationSlot, animationSlot);
        }

        Dictionary<string, string> _debugMap = new Dictionary<string, string>();

        void CreateAnimation( string riderSlot, string mountSlot)
        {
            if (!_debugMap.ContainsKey(riderSlot))
            {
                // Does the rider have this?
                var riderHasAnimation = _riderFragment.Fragments.FirstOrDefault(x => x.Slot.Value == riderSlot) != null;
                if (riderHasAnimation)
                {
                    _debugMap[riderSlot] = "From mount - " + mountSlot;


                    var fragmentEntry = _riderFragment.Fragments.First(x => x.Slot.Value == riderSlot);
                    _riderOutputFragment.Fragments.Add(fragmentEntry.Clone());



                }
                else
                {



                    _debugMap[riderSlot] = "From mount - " + mountSlot + " MISSING! <--";
                }
            }
        }

        void CreateAnimation(string riderSlot)
        {
            if (!_debugMap.ContainsKey(riderSlot))
            {
                _debugMap[riderSlot] = "from self - " + riderSlot;

                var fragmentEntry = _riderFragment.Fragments.First(x => x.Slot.Value == riderSlot);
                _riderOutputFragment.Fragments.Add(fragmentEntry.Clone());
            }
        }

        List<string> GetPoseAndDockAnimationsSlots()
        {
            var poses = _riderFragment.Fragments
              .Where(x => x.Slot.Value.StartsWith("HAND_POSE_", StringComparison.CurrentCultureIgnoreCase) || x.Slot.Value.StartsWith("FACE_POSE", StringComparison.CurrentCultureIgnoreCase))
              .Select(x => x.Slot.Value);

            var docks = _riderFragment.Fragments
              .Where(x => x.Slot.Value.StartsWith("DOCK_", StringComparison.CurrentCultureIgnoreCase))
              .Select(x => x.Slot.Value);

            return poses.Concat(docks).ToList();
        }

        List<string> GetMetaDataSlots()
        {
            return _riderFragment.Fragments
                   .Where(x => x.Slot.Value.StartsWith("PERSISTENT_METADATA_", StringComparison.CurrentCultureIgnoreCase))
                   .Select(x => x.Slot.Value)
                   .ToList();
        }

        List<string> GetPortholeSlots()
        {
            return _riderFragment.Fragments
                  .Where(x => x.Slot.Value.StartsWith("PORTHOLE_", StringComparison.CurrentCultureIgnoreCase))
                  .Select(x => x.Slot.Value)
                  .ToList();
        }

        List<(string, string)> GetRiderMountAnimations()
        {
            return _mountFragment.Fragments
                .Where(x => AnimationSlotTypeHelper.GetMatchingRiderAnimation(x.Slot.Value) != null)
                .Select(x => (AnimationSlotTypeHelper.GetMatchingRiderAnimation(x.Slot.Value).Value, x.Slot.Value))
                .ToList();
        }
    }
}
