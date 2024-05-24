// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System.Linq;
using AssetManagement.Strategies.Fbx.ImportDialog.DataModels;
using Shared.Core.Misc;

namespace AssetManagement.Strategies.Fbx.ImportDialog.ViewModels
{
    public class FBXFileInfoPanelViewModel : NotifyPropertyChangedImpl
    {
        private readonly FbxSettingsModel _inputFbxSettings; // Maybe "FileInfoDat" should store only, and FBXSettings is for "get info from dialog", not something that already contains data, or not?        

        public FBXFileInfoPanelViewModel(FbxSettingsModel inputFbxSettings)
        {
            _inputFbxSettings = inputFbxSettings;
        }

        // -- Asset File Info Panel (maybe should have its own ViewModel?)
        public string FileName
        {
            get { return _inputFbxSettings.FileInfoData.FileName; }
            set
            {
                _inputFbxSettings.FileInfoData.FileName = value;
                NotifyPropertyChanged(nameof(_inputFbxSettings.FileInfoData.FileName));
            }
        }

        public string SdkVersion
        {
            get { return $"{_inputFbxSettings.FileInfoData.SdkVersionUsed.X}.{_inputFbxSettings.FileInfoData.SdkVersionUsed.Y}.{_inputFbxSettings.FileInfoData.SdkVersionUsed.Z}"; }
            set {; }
        }
        public NotifyAttr<string> SkeletonFileName { get; set; } = new NotifyAttr<string>();
        public string SkeletonNodeName { get { return _inputFbxSettings.FileInfoData.SkeletonName; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string Units { get { return _inputFbxSettings.FileInfoData.Units; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MeshCount { get { return $"{_inputFbxSettings.FileInfoData.MeshCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string NodeCount { get { return $"{_inputFbxSettings.FileInfoData.ElementCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string MaterialCount { get { return $"{_inputFbxSettings.FileInfoData.MaterialCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string AnimationCount { get { return $"{_inputFbxSettings.FileInfoData.AnimationsCount}"; } set { /*_fbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged()*/; } }
        public string BoneCount
        {
            get
            {
                // In the FBX FILE "SkeletonName" is encoded in an "fbxsdk::FbxSkeletonNode", as it is the only data that is reliably re-saved from Blender, 
                // so, if the FbxSettings contains a valid skeleton name, it means there is an extra bone, 
                // which  is not part of the "TW ANIM" Skeleton hierachy, in that case subtract that.
                var actualBoneCount =
                    _inputFbxSettings.FileInfoData.BoneCount -
                    (_inputFbxSettings.SkeletonName != null && _inputFbxSettings.SkeletonName.Any() ? 1 : 0);

                return $"{actualBoneCount}";
            }
            set
            {
                _inputFbxSettings.FileInfoData.SkeletonName = value;
                NotifyPropertyChanged();
            }
        }

        public string DerformationData
        {
            get
            {
                return _inputFbxSettings.FileInfoData.ContainsDerformingData ? "Yes" : "No";
            }

            
            set { _inputFbxSettings.FileInfoData.SkeletonName = value; NotifyPropertyChanged(); }
        }


    }
}
