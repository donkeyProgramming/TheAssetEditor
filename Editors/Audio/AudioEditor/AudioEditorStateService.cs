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

        public void StoreAudioProject(AudioProject audioProject);
        public void StoreAudioProjectFileName(string audioProjectFileName);
        public void StoreAudioProjectFilePath(string audioProjectFilePath);
        void StoreSelectedAudioProjectExplorerNode(AudioProjectTreeNode node);
        void StoreModdedStatesOnly(bool moddedStatesOnly);
        void StoreAudioSettings(AudioSettings audioSettings);
        void StoreAudioFiles(List<AudioFile> audioFiles); 
        void StoreSelectedViewerRows(List<DataRow> selectedViewerRows);
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

        public void StoreAudioProject(AudioProject audioProject) => AudioProject = audioProject;

        public void StoreAudioProjectFileName(string audioProjectFileName) => AudioProjectFileName = audioProjectFileName;

        public void StoreAudioProjectFilePath(string audioProjectFilePath) => AudioProjectFilePath = audioProjectFilePath;

        public void StoreSelectedAudioProjectExplorerNode(AudioProjectTreeNode node) => SelectedAudioProjectExplorerNode = node;

        public void StoreModdedStatesOnly(bool showModdedStatesOnly) => ShowModdedStatesOnly = showModdedStatesOnly;

        public void StoreAudioSettings(AudioSettings audioSettings) => AudioSettings = audioSettings;

        public void StoreAudioFiles(List<AudioFile> audioFiles) => AudioFiles = audioFiles;

        public void StoreSelectedViewerRows(List<DataRow> selectedViewerRows) => SelectedViewerRows = selectedViewerRows;

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
