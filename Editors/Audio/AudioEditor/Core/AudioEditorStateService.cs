using System.Collections.Generic;
using System.Data;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.AudioEditor.Core
{
    public interface IAudioEditorStateService
    {
        // Audio Project
        AudioProjectFile AudioProject { get; set; }
        string AudioProjectFileName { get; set; }
        string AudioProjectFilePath { get; set; }

        // Audio Project Explorer
        AudioProjectTreeNode SelectedAudioProjectExplorerNode { get; set; }

        // Audio Project Editor
        bool ShowModdedStatesOnly { get; set; }

        // Settings
        HircSettings HircSettings { get; set; }
        List<AudioFile> AudioFiles { get; set; }

        // Audio Project Viewer
        List<DataRow> SelectedViewerRows { get; set; }
        List<DataRow> CopiedViewerRows { get; set; }

        public void StoreAudioProject(AudioProjectFile audioProject);
        public void StoreAudioProjectFileName(string audioProjectFileName);
        public void StoreAudioProjectFilePath(string audioProjectFilePath);
        void StoreSelectedAudioProjectExplorerNode(AudioProjectTreeNode node);
        void StoreModdedStatesOnly(bool moddedStatesOnly);
        void StoreHircSettings(HircSettings hircSettings);
        void StoreAudioFiles(List<AudioFile> audioFiles); 
        void StoreSelectedViewerRows(List<DataRow> selectedViewerRows);
        void StoreCopiedViewerRows(List<DataRow> copiedViewerRows);
        void Reset();
    }

    public class AudioEditorStateService : IAudioEditorStateService
    {
        public AudioProjectFile AudioProject { get; set; }
        public string AudioProjectFileName { get; set; }
        public string AudioProjectFilePath { get; set; }
        public AudioProjectTreeNode SelectedAudioProjectExplorerNode { get; set; }
        public bool ShowModdedStatesOnly { get; set; }
        public HircSettings HircSettings { get; set; }
        public List<AudioFile> AudioFiles { get; set; } = [];
        public List<DataRow> SelectedViewerRows { get; set; }
        public List<DataRow> CopiedViewerRows { get; set; }

        public void StoreAudioProject(AudioProjectFile audioProject) => AudioProject = audioProject;

        public void StoreAudioProjectFileName(string audioProjectFileName) => AudioProjectFileName = audioProjectFileName;

        public void StoreAudioProjectFilePath(string audioProjectFilePath) => AudioProjectFilePath = audioProjectFilePath;

        public void StoreSelectedAudioProjectExplorerNode(AudioProjectTreeNode node) => SelectedAudioProjectExplorerNode = node;

        public void StoreModdedStatesOnly(bool showModdedStatesOnly) => ShowModdedStatesOnly = showModdedStatesOnly;

        public void StoreHircSettings(HircSettings hircSettings) => HircSettings = hircSettings;

        public void StoreAudioFiles(List<AudioFile> audioFiles) => AudioFiles = audioFiles;

        public void StoreSelectedViewerRows(List<DataRow> selectedViewerRows) => SelectedViewerRows = selectedViewerRows;

        public void StoreCopiedViewerRows(List<DataRow> copiedViewerRows) => CopiedViewerRows = copiedViewerRows;

        public void Reset()
        {
            AudioProject = null;
            AudioProjectFileName = null;
            AudioProjectFilePath = null;
            SelectedAudioProjectExplorerNode = null;
            ShowModdedStatesOnly = false;
            HircSettings = null;
            AudioFiles.Clear();
            SelectedViewerRows = null;
        }
    }
}
