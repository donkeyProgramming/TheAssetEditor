using System.Collections.Generic;
using System.Windows.Controls;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.DataGrids.CellTemplates;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class SingleRowDataGridConfiguration
    {
        public static void ConfigureAudioProjectEditorSingleRowDataGridForModdedStates(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, string moddedStateGroup)
        {
            var dataGrid = GetDataGridByTag(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);

            // Column for the Modded States
            var stateGroupColumn = new DataGridTemplateColumn
            {
                Header = AddExtraUnderscoresToString(moddedStateGroup),
                CellTemplate = CreateEditableTextBoxTemplate(audioEditorViewModel, audioProjectService, audioRepository, AddExtraUnderscoresToString(moddedStateGroup)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public static void ConfigureAudioProjectEditorSingleRowDataGridForActionEventSoundBank(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var dataGrid = GetDataGridByTag(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);

            var columnsCount = 3;
            var columnWidth = columnsCount > 0 ? 1.0 / columnsCount : 1.0;

            // Column for the Event
            var columnHeader = "Event";
            var eventNameColumn = new DataGridTemplateColumn
            {
                Header = columnHeader,
                CellTemplate = CreateEditableTextBoxTemplate(audioEditorViewModel, audioProjectService, audioRepository, columnHeader),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star)
            };
            dataGrid.Columns.Add(eventNameColumn);

            // Column for Audio files TextBox with Tooltip
            var soundsTextBoxColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                CellTemplate = CreateSoundsTextBoxTemplate(),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
            };
            dataGrid.Columns.Add(soundsTextBoxColumn);

            // Create and set the tooltip binding
            var soundsButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateSoundsButtonTemplate(audioEditorViewModel, audioProjectService, audioRepository),
                Width = 30.0,
                CanUserResize = false
            };
            dataGrid.Columns.Add(soundsButtonColumn);
        }

        public static void ConfigureAudioProjectEditorSingleRowDataGridForDialogueEvent(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, DialogueEvent dialogueEvent, IAudioProjectService audioProjectService)
        {
            var dataGrid = GetDataGridByTag(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorSingleRowDataGridTag);

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[dialogueEvent.Name];
            var stateGroupsWithQualifiers = audioRepository.DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name];

            var stateGroupsCount = stateGroups.Count + 1;
            var columnWidth = stateGroupsCount > 0 ? 1.0 / stateGroupsCount : 1.0;

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var stateGroupWithQualifierWithExtraUnderscores = AddExtraUnderscoresToString(stateGroupWithQualifier);
                var stateGroup = kvp.Value;

                var states = new List<string>();
                var customStates = new List<string>();

                var vanillaStates = audioRepository.StateGroupsWithStates[stateGroup];

                if (audioProjectService.StateGroupsWithModdedStatesRepository != null && audioProjectService.StateGroupsWithModdedStatesRepository.Count > 0)
                {
                    if (ModdedStateGroups.Contains(stateGroup) && audioProjectService.StateGroupsWithModdedStatesRepository.ContainsKey(stateGroup))
                        customStates = audioProjectService.StateGroupsWithModdedStatesRepository[stateGroup];
                }

                if (audioEditorViewModel.ShowModdedStatesOnly && ModdedStateGroups.Contains(stateGroup))
                {
                    states.Add("Any");
                    states.AddRange(customStates);
                }
                else
                {
                    if (ModdedStateGroups.Contains(stateGroup))
                        states.AddRange(customStates);

                    states.AddRange(vanillaStates);
                }

                var column = new DataGridTemplateColumn
                {
                    Header = stateGroupWithQualifierWithExtraUnderscores,
                    CellTemplate = CreateStatesComboBoxTemplate(audioEditorViewModel, states, stateGroupWithQualifierWithExtraUnderscores, audioProjectService, audioRepository),
                    Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                };
                dataGrid.Columns.Add(column);
            }

            var soundsTextBoxColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                CellTemplate = CreateSoundsTextBoxTemplate(),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
            };
            dataGrid.Columns.Add(soundsTextBoxColumn);

            var soundsButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateSoundsButtonTemplate(audioEditorViewModel, audioProjectService, audioRepository),
                Width = 30.0,
                CanUserResize = false
            };
            dataGrid.Columns.Add(soundsButtonColumn);
        }
    }
}
