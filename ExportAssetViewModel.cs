using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AssetManagement.Strategies.Fbx.ImportDialog.DataModels;
using CommonControls.Common;

namespace AssetManagement.Strategies.Fbx.ImportDialog.ViewModels
{
    public class ExportAssetViewModel : NotifyPropertyChangedImpl
    {
        private readonly FbxSettingsModel _inputFbxSettings;

        public ExportAssetViewModel(FbxSettingsModel inputFbxSettings)
        {
            _inputFbxSettings = inputFbxSettings;
        }

        public string ExportPath
        {
            get { return _inputFbxSettings.ExportPath; }
            set
            {
                _inputFbxSettings.ExportPath = value;
                NotifyPropertyChanged(nameof(ExportPath));
            }
        }

        public ICommand ExportCommand { get; set; }

        private void Export()
        {
            // TODO: Implement export logic
        }
    }
}
