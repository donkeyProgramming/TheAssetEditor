using CommonControls.FileTypes.AnimationPack;
using System;
using System.Collections.Generic;

namespace Wh3AnimPackCreator
{
    public class ResourecSwapRules
    {
        protected BaseAnimationSlotHelper _targetGame;
        protected Dictionary<string, string> _animationSlots = new Dictionary<string, string>();
        protected Dictionary<string, string> _vfx = new Dictionary<string, string>();

        public ResourecSwapRules(BaseAnimationSlotHelper slotHelper)
        {
            _targetGame = slotHelper;
        }

        public string GetMatchingAnimationSlotName(string inputSlot)
        {
            if (_targetGame.GetfromValue(inputSlot) != null)
                return inputSlot;
                
            if (_animationSlots.ContainsKey(inputSlot))
            {
                Console.WriteLine($"\t\t\t Converting AnimationSlot {inputSlot} => {_animationSlots[inputSlot]}");
                return _animationSlots[inputSlot];
            }

            Console.WriteLine($"\t\t\t Skipping AnimationSlot {inputSlot}");
            return null;
        }

        public string GetmatchingVfx(string inputVfx)
        {
            if (_vfx.ContainsKey(inputVfx))
            {
                Console.WriteLine($"\t\t\t Converting VFX {inputVfx} => {_vfx[inputVfx]}");
                return _vfx[inputVfx];
            }

            Console.WriteLine($"\t\t\t Skipping VFX {inputVfx}");
            return null;
        }
    }

    public class TroyResourceSwapRules : ResourecSwapRules
    {
        public TroyResourceSwapRules() 
            : base(new BaseAnimationSlotHelper(CommonControls.Services.GameTypeEnum.Warhammer3))
        {
            _animationSlots["SPECIAL_ABILITY_SAVAGE_ROAR"] = "CAST_SPELL_FORWARD_MEDIUM";
        }

    }
}
