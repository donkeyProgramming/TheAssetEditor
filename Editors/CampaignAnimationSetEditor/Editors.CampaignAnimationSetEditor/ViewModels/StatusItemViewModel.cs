using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shared.GameFormats.AnimationPack;

namespace Editors.CampaignAnimationSetEditor.ViewModels
{
    using StatusItem = CampaignAnimationBin.StatusItem;
    using AnimationEntry = CampaignAnimationBin.AnimationEntry;
    using TransitionEntry = CampaignAnimationBin.TransitionEntry;
    using ActionEntry = CampaignAnimationBin.ActionEntry;
    using LocomotionEntry = CampaignAnimationBin.LocomotionEntry;
    using PortholeEntry = CampaignAnimationBin.PortholeEntry;
    using UnknownEntry = CampaignAnimationBin.UnknownEntry;
    using PersistentMeta = CampaignAnimationBin.PersistentMeta;
    using PersistentMeta_Pose = CampaignAnimationBin.PersistentMeta_Pose;
    using PersistentMeta_Dock = CampaignAnimationBin.PersistentMeta_Dock;

    /// <summary>Wraps one <see cref="StatusItem"/> for the editor grid UI. The binary format stores
    /// either the "global" lists (persistent metadata/poses/docks) or the normal status lists
    /// (idle/selection/transitions/action/locomotion/...) depending on the status name - see
    /// <see cref="CampaignAnimationBinLoader.LoadStatus"/> - so <see cref="IsGlobal"/> is fixed at
    /// construction and switches which set of collections the view shows.</summary>
    public partial class StatusItemViewModel : ObservableObject
    {
        public StatusItem Model { get; }

        public bool IsGlobal { get; }
        public bool IsNormal => !IsGlobal;

        /// <summary>Default for a new entry's Type field. Vanilla only ever uses "global" or
        /// "status_normal" here, never the containing status's own name - a status_battle entry
        /// normally reads Type=status_normal, meaning it reuses status_normal's authored animation.</summary>
        string DefaultEntryType => IsGlobal ? "global" : "status_normal";

        [ObservableProperty] string _name;

        // Global-only lists
        public ObservableCollection<PersistentMeta> PersistentMetaData { get; }
        public ObservableCollection<PersistentMeta_Pose> Poses { get; }
        public ObservableCollection<PersistentMeta_Dock> Docks { get; }

        // Normal status lists
        public ObservableCollection<AnimationEntry> Idle { get; }
        public ObservableCollection<PortholeEntry> Porthole { get; }
        public ObservableCollection<AnimationEntry> Selection { get; }
        public ObservableCollection<TransitionEntry> Transitions { get; }
        public ObservableCollection<ActionEntry> Action { get; }
        public ObservableCollection<UnknownEntry> Unknown { get; }
        public ObservableCollection<LocomotionEntry> Locomotion { get; }

        public StatusItemViewModel(StatusItem model)
        {
            Model = model;
            _name = model.Name;
            IsGlobal = model.Name == "global";

            PersistentMetaData = new ObservableCollection<PersistentMeta>(model.PersitantMetaData ?? []);
            Poses = new ObservableCollection<PersistentMeta_Pose>(model.Poses ?? []);
            Docks = new ObservableCollection<PersistentMeta_Dock>(model.Docks ?? []);

            Idle = new ObservableCollection<AnimationEntry>(model.Idle ?? []);
            Porthole = new ObservableCollection<PortholeEntry>(model.Porthole ?? []);
            Selection = new ObservableCollection<AnimationEntry>(model.Selection ?? []);
            Transitions = new ObservableCollection<TransitionEntry>(model.Transitions ?? []);
            Action = new ObservableCollection<ActionEntry>(model.Action ?? []);
            Unknown = new ObservableCollection<UnknownEntry>(model.Unknown ?? []);
            Locomotion = new ObservableCollection<LocomotionEntry>(model.Locomotion ?? []);
        }

        /// <summary>Creates a brand new, empty status. <paramref name="name"/> "global" (case
        /// sensitive, matching the loader) gets the global-only lists; anything else gets the
        /// normal status lists.</summary>
        public static StatusItemViewModel CreateNew(string name)
        {
            return new StatusItemViewModel(new StatusItem { Name = name });
        }

        /// <summary>Writes the (possibly edited) collections back into the underlying model. Called
        /// before the bin is serialized.</summary>
        public void CommitToModel()
        {
            Model.Name = Name;

            if (IsGlobal)
            {
                Model.PersitantMetaData = PersistentMetaData.ToList();
                Model.Poses = Poses.ToList();
                Model.Docks = Docks.ToList();
            }
            else
            {
                Model.Idle = Idle.ToList();
                Model.Porthole = Porthole.ToList();
                Model.Selection = Selection.ToList();
                Model.Transitions = Transitions.ToList();
                Model.Action = Action.ToList();
                Model.Unknown = Unknown.ToList();
                Model.Locomotion = Locomotion.ToList();
            }
        }

        [RelayCommand] void AddPersistentMetaData() => PersistentMetaData.Add(new PersistentMeta { Type = DefaultEntryType });
        [RelayCommand] void RemovePersistentMetaData(PersistentMeta? item) { if (item != null) PersistentMetaData.Remove(item); }

        [RelayCommand] void AddPose() => Poses.Add(new PersistentMeta_Pose { Type = DefaultEntryType, BlendTime = 0.5f });
        [RelayCommand] void RemovePose(PersistentMeta_Pose? item) { if (item != null) Poses.Remove(item); }

        [RelayCommand] void AddDock() => Docks.Add(new PersistentMeta_Dock { Type = DefaultEntryType, Dock = "DOCK_EQPT_RHAND", BlendTime = 0.5f });
        [RelayCommand] void RemoveDock(PersistentMeta_Dock? item) { if (item != null) Docks.Remove(item); }

        [RelayCommand] void AddIdle() => Idle.Add(new AnimationEntry { Type = DefaultEntryType, BlendTime = 0.5f });
        [RelayCommand] void RemoveIdle(AnimationEntry? item) { if (item != null) Idle.Remove(item); }

        [RelayCommand] void AddPorthole() => Porthole.Add(new PortholeEntry { Type = DefaultEntryType, BlendTime = 0.5f });
        [RelayCommand] void RemovePorthole(PortholeEntry? item) { if (item != null) Porthole.Remove(item); }

        [RelayCommand] void AddSelection() => Selection.Add(new AnimationEntry { Type = DefaultEntryType, BlendTime = 0.5f });
        [RelayCommand] void RemoveSelection(AnimationEntry? item) { if (item != null) Selection.Remove(item); }

        [RelayCommand] void AddTransition() => Transitions.Add(new TransitionEntry { Type = DefaultEntryType, BlendTime = 0.5f });
        [RelayCommand] void RemoveTransition(TransitionEntry? item) { if (item != null) Transitions.Remove(item); }

        [RelayCommand]
        void AddAction()
        {
            // ActionId behaves like a row counter rather than a semantic code (see KnownActionType),
            // so it's auto-assigned instead of left for the user to guess.
            var nextActionId = Action.Count > 0 ? Action.Max(x => x.ActionId) + 1 : 1;
            Action.Add(new ActionEntry { Type = DefaultEntryType, BlendTime = 0.5f, ActionId = nextActionId });
        }
        [RelayCommand] void RemoveAction(ActionEntry? item) { if (item != null) Action.Remove(item); }

        [RelayCommand] void AddUnknown() => Unknown.Add(new UnknownEntry());
        [RelayCommand] void RemoveUnknown(UnknownEntry? item) { if (item != null) Unknown.Remove(item); }

        [RelayCommand] void AddLocomotion() => Locomotion.Add(new LocomotionEntry { Type = DefaultEntryType, BlendTime = 0.5f, ModelScale = 1 });
        [RelayCommand] void RemoveLocomotion(LocomotionEntry? item) { if (item != null) Locomotion.Remove(item); }
    }
}
