using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Editors.Audio.Shared.Wwise
{
    public enum ContainerType
    {
        [Display(Name = "Random")] Random,
        [Display(Name = "Sequence")] Sequence
    }

    public enum RandomType
    {
        [Display(Name = "Standard")] Standard,
        [Display(Name = "Shuffle")] Shuffle
    }

    public enum PlayMode
    {
        [Display(Name = "Step")] Step,
        [Display(Name = "Continuous")] Continuous
    }

    public enum PlaylistEndBehaviour
    {
        [Display(Name = "Restart")] Restart,
        [Display(Name = "Play In Reverse Order")] PlayInReverseOrder
    }

    public enum LoopingType
    {
        [Display(Name = "Disabled")] Disabled,
        [Display(Name = "Finite Looping")] FiniteLooping,
        [Display(Name = "Infinite Looping")] InfiniteLooping,
    }

    public enum TransitionType
    {
        [Display(Name = "Disabled")] Disabled,
        [Display(Name = "Xfade (amp)")] XfadeAmp,
        [Display(Name = "Xfade (power)")] XfadePower,
        [Display(Name = "Delay")] Delay,
        [Display(Name = "Sample Accurate")] SampleAccurate,
        [Display(Name = "Trigger Rate")] TriggerRate
    }

    public class HircSettings
    {
        public static string GetEnumDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var field = typeof(TEnum).GetField(value.ToString());
            var display = field?.GetCustomAttribute<DisplayAttribute>();
            if (display != null)
                return display.GetName();
            return value.ToString();
        }

        public static List<string> GetAllDisplayNamesFor<TEnum>() where TEnum : struct, Enum
        {
            var names = new List<string>();
            foreach (var enumValue in Enum.GetValues<TEnum>())
                names.Add(GetEnumDisplayName(enumValue));
            return names;
        }
    }
}
