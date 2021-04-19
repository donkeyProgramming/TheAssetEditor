using Common;
using Common.ApplicationSettings;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class ModelSceneNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        string _fileName;
        public string FileName { get { return _fileName; } set { SetAndNotify(ref _fileName, value); } }

        string _selectedVersion;
        public string SelectedVersion { get { return _selectedVersion; } set { SetAndNotify(ref _selectedVersion, value); } }


        ObservableCollection<RmvAttachmentPoint> _attachmentPoints;
        public ObservableCollection<RmvAttachmentPoint> AttachmentPoints { get { return _attachmentPoints; } set { SetAndNotify(ref _attachmentPoints, value); } }

        Rmv2ModelNode _modelNode;

        public ModelSceneNodeViewModel(Rmv2ModelNode node)
        {
            _modelNode = node;

             FileName = node.Model.FileName;
            SelectedVersion = "7";

            // Find all attachmentpoints
            var attachmentPoints = node.Model.MeshList.SelectMany(x => x.SelectMany(y => y.AttachmentPoints));
            attachmentPoints = attachmentPoints.DistinctBy(x => x.BoneIndex);
            attachmentPoints = attachmentPoints.OrderBy(x => x.BoneIndex);
            AttachmentPoints = new ObservableCollection<RmvAttachmentPoint>(attachmentPoints);

            // Ensure all models have this value set
            UpdateAttachmentPoint();
        }

        void UpdateAttachmentPoint()
        {
            ModelEditorService service = new ModelEditorService(_modelNode);
            service.SetAttachmentPoints(AttachmentPoints.ToList());
        }

        public void Dispose()
        {

        }
    }
}
