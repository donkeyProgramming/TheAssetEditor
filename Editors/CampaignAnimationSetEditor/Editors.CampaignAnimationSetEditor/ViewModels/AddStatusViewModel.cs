using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.CampaignAnimationSetEditor.Services;

namespace Editors.CampaignAnimationSetEditor.ViewModels
{
    /// <summary>Backs the "Add status" dialog. The combo box is editable - the known vanilla
    /// status names (<see cref="KnownCampaignStatus"/>) are offered as suggestions, but custom/
    /// modded status names are still accepted since the game doesn't enforce a fixed set.</summary>
    public partial class AddStatusViewModel : ObservableObject
    {
        public ObservableCollection<string> KnownStatuses { get; }

        [ObservableProperty] string _statusName = "";

        public AddStatusViewModel(IEnumerable<string> existingStatusNames)
        {
            var existing = new HashSet<string>(existingStatusNames);
            var names = Enum.GetNames<KnownCampaignStatus>().Where(x => !existing.Contains(x));
            KnownStatuses = new ObservableCollection<string>(names);
        }
    }
}
