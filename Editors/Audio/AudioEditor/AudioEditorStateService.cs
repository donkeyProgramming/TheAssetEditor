using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioEditorStateService
    {
        // Audio Project
        AudioProject AudioProject { get; set; }
        string AudioProjectFileName { get; set; }
        string AudioProjectFilePath { get; set; }

        // Audio Project Explorer
        AudioProjectTreeNode SelectedAudioProjectExplorerNode { get; set; }

        // Audio Project Editor
        bool ShowModdedStatesOnly { get; set; }

        // Audio Project Settings
        AudioSettings AudioSettings { get; set; }
        List<AudioFile> AudioFiles { get; set; }

        // Audio Project Viewer
        List<DataRow> SelectedViewerRows { get; set; }

        void Reset();
    }

    public class AudioEditorStateService : IAudioEditorStateService
    {
        public AudioProject AudioProject { get; set; }
        public string AudioProjectFileName { get; set; }
        public string AudioProjectFilePath { get; set; }
        public AudioProjectTreeNode SelectedAudioProjectExplorerNode { get; set; }
        public bool ShowModdedStatesOnly { get; set; }
        public AudioSettings AudioSettings { get; set; }
        public List<AudioFile> AudioFiles { get; set; } = [];
        public List<DataRow> SelectedViewerRows { get; set; }

        public void Reset()
        {
            AudioProject = null;
            AudioProjectFileName = null;
            AudioProjectFilePath = null;
            SelectedAudioProjectExplorerNode = null;
            ShowModdedStatesOnly = false;
            AudioSettings = null;
            AudioFiles.Clear();
            SelectedViewerRows = null;
        }
    }
}
