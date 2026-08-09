using System;
using System.Globalization;
using System.Windows.Data;
using Shared.GameFormats.AnimationPack;

namespace Editors.CampaignAnimationSetEditor.Views
{
    /// <summary>Reads <see cref="IAnimationBinGenericFormat.FullPath"/> through the interface.
    /// DisplayMemberPath="FullPath" can't see it - AnimationBinWh3 implements it as an explicit
    /// interface member, invisible to reflection-based binding on the concrete type.</summary>
    public class AnimationFragmentDisplayConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is IAnimationBinGenericFormat fragment ? fragment.FullPath : value?.ToString();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
