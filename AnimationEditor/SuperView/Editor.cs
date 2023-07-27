using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationMeta.Presentation;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace AnimationEditor.SuperView
{
    public class Editor
    {
        SceneObject _asset;

        private readonly PackFileService _pfs;
        private readonly AnimationPlayerViewModel _player;
        private readonly SceneObjectBuilder _sceneObjectBuilder;

        public NotifyAttr<string> PersistentMetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> PersistentMetaFilePackFileContainerName { get; set; } = new NotifyAttr<string>("");

        public NotifyAttr<string> MetaFilePath { get; set; } = new NotifyAttr<string>("");
        public NotifyAttr<string> MetaFilePackFileContainerName { get; set; } = new NotifyAttr<string>("");

        public EditorViewModel PersistentMetaEditor { get; private set; }
        public EditorViewModel MetaEditor { get; private set; }

        public Editor(SceneObjectBuilder sceneObjectBuilder, PackFileService pfs,  AnimationPlayerViewModel player, CopyPasteManager copyPasteManager)
        {
            _sceneObjectBuilder = sceneObjectBuilder;
            _pfs = pfs;
            _player = player;

            PersistentMetaEditor = new EditorViewModel(pfs, copyPasteManager);
            PersistentMetaEditor.EditorSavedEvent += PersistentMetaEditor_EditorSavedEvent;

            MetaEditor = new EditorViewModel(pfs, copyPasteManager);
            MetaEditor.EditorSavedEvent += MetaEditor_EditorSavedEvent;
        }

        public void Create(SceneObject asset)
        {
            _asset = asset;
            _player.RegisterAsset(_asset);

            _asset.MetaDataChanged += Asset_MetaDataChanged;
            Asset_MetaDataChanged(_asset);
        }

        private void Asset_MetaDataChanged(SceneObject newValue)
        {
            PersistentMetaEditor.MainFile = newValue.PersistMetaData;
            if (PersistentMetaEditor.MainFile != null)
            {
                PersistentMetaFilePackFileContainerName.Value = _pfs.GetPackFileContainer(PersistentMetaEditor.MainFile).Name;
                PersistentMetaFilePath.Value = _pfs.GetFullPath(PersistentMetaEditor.MainFile);
            }
            else
            {
                PersistentMetaFilePath.Value = "";
                PersistentMetaFilePackFileContainerName.Value = "";
            }

            MetaEditor.MainFile = newValue.MetaData;
            if (MetaEditor.MainFile != null)
            {
                MetaFilePackFileContainerName.Value = _pfs.GetPackFileContainer(MetaEditor.MainFile).Name;
                MetaFilePath.Value = _pfs.GetFullPath(MetaEditor.MainFile);
            }
            else
            {
                MetaFilePath.Value = "";
                MetaFilePackFileContainerName.Value = "";
            }
        }

        private void MetaEditor_EditorSavedEvent(PackFile newFile)
        {
            _sceneObjectBuilder.SetMetaFile(_asset, newFile, _asset.PersistMetaData);
        }

        private void PersistentMetaEditor_EditorSavedEvent(PackFile newFile)
        {
            _sceneObjectBuilder.SetMetaFile(_asset, _asset.MetaData, newFile);
        }

        public void RefreshAction()
        {
            _asset.TriggerMeshChanged();
        }
    }
}
