using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject;

namespace Editors.Audio.AudioEditor
{
    public class StatesProjectData
    {
        public class StatesProject
        {
            public List<StatesProjectItems> StatesProjectItems { get; set; } = [];
        }

        public class StatesProjectItems
        {
            public List<StateGroupStatePair> StatesProjectItem { get; set; } = [];
        }

        public class StateGroupStatePair
        {
            public string StateGroup { get; set; }
            public string State { get; set; }
        }

        public static readonly List<string> ModdedStateGroups = ["VO_Actor", "VO_Culture", "VO_Faction_Leader", "VO_Battle_Selection", "VO_Battle_Special_Ability"];

        public static void ConvertDataGridDataToStatesProject(ObservableCollection<Dictionary<string, object>> dataGridData)
        {
            if (dataGridData.Count() == 0)
                return;

            var statesProject = AudioProjectInstance.StatesProject;
            statesProject.StatesProjectItems = new List<StatesProjectItems>();

            foreach (var dataGridItem in dataGridData)
            {
                var statesProjectItem = new StatesProjectItems
                {
                    StatesProjectItem = new List<StateGroupStatePair>()
                };

                foreach (var kvp in dataGridItem)
                {
                    var stateGroup = RemoveExtraUnderscoresFromString(kvp.Key);
                    var state = kvp.Value.ToString();

                    var stateGroupStatePair = new StateGroupStatePair
                    {
                        StateGroup = stateGroup,
                        State = state
                    };

                    statesProjectItem.StatesProjectItem.Add(stateGroupStatePair);
                }

                statesProject.StatesProjectItems.Add(statesProjectItem);
            }
        }

        public static void ConvertStatesProjectToDataGridData(ObservableCollection<Dictionary<string, object>> dataGridData, StatesProject statesProject)
        {
            foreach (var statesProjectItem in statesProject.StatesProjectItems)
            {
                var dataGridRow = new Dictionary<string, object>();

                foreach (var stateGroupStatePair in statesProjectItem.StatesProjectItem)
                {
                    var stateGroup = AddExtraUnderscoresToString(stateGroupStatePair.StateGroup);
                    var state = stateGroupStatePair.State;

                    dataGridRow[stateGroup] = state;
                }

                dataGridData.Add(dataGridRow);
            }
        }
    }
}
