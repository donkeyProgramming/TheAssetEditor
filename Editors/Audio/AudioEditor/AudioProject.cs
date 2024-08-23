using System;
using System.Collections.Generic;
using System.IO;
using static Editors.Audio.AudioEditor.StatesProjectData;
using static Editors.Audio.AudioEditor.VOProjectData;

namespace Editors.Audio.AudioEditor
{
    public class AudioProject
    {
        public enum ProjectType
        {
            sfxaproj,
            voaproj,
            statesaproj
        }

        private static readonly AudioProject _instance = new();

        public static AudioProject AudioProjectInstance => _instance;

        public VOProject VOProject { get; set; } = new VOProject();

        public StatesProject StatesProject { get; set; } = new StatesProject();

        public static Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = new();

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = new();

        public ProjectType? Type { get; set; }

        public string FileName { get; set; }

        public string Directory { get; set; }

        public string SelectedAudioProjectEvent { get; set; }

        public string PreviousSelectedAudioProjectEvent { get; set; }

        private AudioProject()
        {
        }

        public void ResetAudioProjectData()
        {
            VOProject = null;
            StatesProject = null;
            DialogueEventsWithStateGroupsWithQualifiers = null;
            StateGroupsWithCustomStates = null;
            Type = null;
            FileName = null;
            Directory = null;
            SelectedAudioProjectEvent = null;
            PreviousSelectedAudioProjectEvent = null;
        }
    }
}
