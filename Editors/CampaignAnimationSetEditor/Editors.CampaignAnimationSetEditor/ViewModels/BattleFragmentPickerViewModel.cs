using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.AnimationPack;

namespace Editors.CampaignAnimationSetEditor.ViewModels
{
    /// <summary>Lists every battle animation fragment from all loaded .animpack files for the
    /// "Generate from Battle Animation Set" picker. Deliberately not filtered to the open file's
    /// skeleton - regenerating from a different skeleton is legitimate, and Generate() overwrites
    /// the bin's SkeletonName from whichever fragment is picked.</summary>
    public partial class BattleFragmentPickerViewModel : ObservableObject
    {
        public ObservableCollection<IAnimationBinGenericFormat> Fragments { get; } = [];

        [ObservableProperty] IAnimationBinGenericFormat? _selectedFragment;
        [ObservableProperty] string _filterText = "";

        /// <summary>Riders only - see <see cref="Services.CampaignAnimationSetGeneratorService.Generate"/>.</summary>
        [ObservableProperty] bool _freezeRootForRiderAnimations;

        /// <summary>Generates a flying-capable unit with its ground moveset instead - see
        /// <see cref="Services.CampaignAnimationSetGeneratorService.Generate"/>.</summary>
        [ObservableProperty] bool _forceGroundAnimations;

        readonly List<IAnimationBinGenericFormat> _allFragments;

        public BattleFragmentPickerViewModel(IPackFileService packFileService)
        {
            _allFragments = [];

            foreach (var animPackFile in PackFileServiceUtility.GetAllAnimPacks(packFileService))
            {
                var database = AnimationPackSerializer.Load(animPackFile, packFileService);
                _allFragments.AddRange(database.GetGenericAnimationSets());
            }

            foreach (var fragment in _allFragments.OrderBy(x => x.FullPath))
                Fragments.Add(fragment);
        }

        partial void OnFilterTextChanged(string value)
        {
            Fragments.Clear();
            var matches = string.IsNullOrWhiteSpace(value)
                ? _allFragments
                : _allFragments.Where(x => x.FullPath.Contains(value, System.StringComparison.OrdinalIgnoreCase));

            foreach (var fragment in matches.OrderBy(x => x.FullPath))
                Fragments.Add(fragment);
        }
    }
}
