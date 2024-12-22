using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Ui.Common;

namespace Editors.AnimationVisualEditors.AnimationTransferTool.Saving
{
    public partial class SaveViewModel : ObservableObject
    {
    }

    public partial class SaveSettings : ObservableObject
    {
        public List<uint> PossibleAnimationFormats = [5, 6, 7];

        [ObservableProperty] string _savePrefix = "prefix_";
        [ObservableProperty] uint _animationFormat = 7;
        [ObservableProperty] bool _useGeneratedSkeleton = false;
        [ObservableProperty] string _scaledSkeletonName = "";
    }
}
