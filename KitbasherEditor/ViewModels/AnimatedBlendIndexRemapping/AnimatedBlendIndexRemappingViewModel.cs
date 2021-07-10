using Common;
using Common.ApplicationSettings;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping
{
    public class AnimatedBlendIndexRemappingViewModel : NotifyPropertyChangedImpl
    {
        public SkeletonBoneCollection MeshBones { get; set; }
        public SkeletonBoneCollection ParnetModelBones { get; set; }

        string _currentConfigPath = string.Empty;
        public string CurrentConfigPath
        {
            get { return _currentConfigPath; }
            set { OnConfigPathChanged(value);  SetAndNotify(ref _currentConfigPath, value);  }
        }

        public ObservableCollection<string> AllConfigPaths { get; set; }

        RemappedAnimatedBoneConfiguration _configuration;
        ILogger _logger = Logging.Create<AnimatedBlendIndexRemappingViewModel>();

        public AnimatedBlendIndexRemappingViewModel(RemappedAnimatedBoneConfiguration configuration)
        {
            MeshBones = new SkeletonBoneCollection();
            ParnetModelBones = new SkeletonBoneCollection();
            ParnetModelBones.BoneSelected += OnParentBoneSelected;

            CreateFromConfiguration(configuration);
            FindApplicableSettingsFiles();
        }

        public virtual void ClearBindingSelfAndChildren()
        {
            if (MeshBones.SelectedBone == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedBone.ClearMapping();
        }

        public virtual void AutoMapSelfAndChildrenByName()
        {
            if (MeshBones.SelectedBone == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnNames(MeshBones.SelectedBone, ParnetModelBones.Bones);
        }

        public virtual void AutoMapSelfAndChildrenByHierarchy()
        {
            if (MeshBones.SelectedBone == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }
            if (MeshBones.SelectedBone == null)
            {
                MessageBox.Show("No parent model bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnHierarchy(MeshBones.SelectedBone, ParnetModelBones.SelectedBone);
        }


        void CreateFromConfiguration(RemappedAnimatedBoneConfiguration config)
        {
            MeshBones.Bones = config.MeshBones;
            MeshBones.SkeletonName = config.MeshSkeletonName;

            ParnetModelBones.Bones = config.ParentModelBones;
            ParnetModelBones.SkeletonName = config.ParnetModelSkeletonName;

            _configuration = config;
        }

        private void OnParentBoneSelected(AnimatedBone bone)
        {
            MeshBones.SelectedBone.MappedBoneIndex = bone.BoneIndex;
            MeshBones.SelectedBone.MappedBoneName = bone.Name;

            OnMappingCreated(MeshBones.SelectedBone.BoneIndex, MeshBones.SelectedBone.MappedBoneIndex);
        }

        public void Save()
        {
            var dataStr = JsonConvert.SerializeObject(_configuration, Formatting.Indented);

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.InitialDirectory = DirectoryHelper.AnimationIndexMappingDirectory;
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.Filter = "Mapping files(*.json)|*.json;";
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, dataStr);
                if (AllConfigPaths.Contains(saveFileDialog.FileName) == false)
                    AllConfigPaths.Add(saveFileDialog.FileName);

                CurrentConfigPath = saveFileDialog.FileName;
            }
        }

        public void Load()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = DirectoryHelper.AnimationIndexMappingDirectory;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var content = File.ReadAllText(dialog.FileName);
                    var obj = JsonConvert.DeserializeObject<RemappedAnimatedBoneConfiguration>(content);

                    if (obj.MeshSkeletonName == _configuration.MeshSkeletonName && obj.ParnetModelSkeletonName == _configuration.ParnetModelSkeletonName)
                        CreateFromConfiguration(obj);
                    else
                        MessageBox.Show("Unable to open file - The file has config for other skeletons");
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e.ToString());
                    MessageBox.Show("Unable to open file");
                }
            }
        }

        void FindApplicableSettingsFiles()
        {
            AllConfigPaths = new ObservableCollection<string>();
            var files = Directory.GetFiles(DirectoryHelper.AnimationIndexMappingDirectory);
            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var obj = JsonConvert.DeserializeObject<RemappedAnimatedBoneConfiguration>(content);

                    if (obj.MeshSkeletonName == _configuration.MeshSkeletonName && obj.ParnetModelSkeletonName == _configuration.ParnetModelSkeletonName)
                        AllConfigPaths.Add(file);
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e.ToString());
                }
            }
        }

        void OnConfigPathChanged(string filename)
        {
            if (filename == _currentConfigPath)
                return;

            var content = File.ReadAllText(filename);
            var obj = JsonConvert.DeserializeObject<RemappedAnimatedBoneConfiguration>(content);
            CreateFromConfiguration(obj);
        }

        public virtual void OnMappingCreated(int originalBoneIndex, int newBoneIndex)
        { }

        public virtual bool Validate(out string errorText)
        {
            var usedBonesCount = MeshBones.GetUsedBonesCount();
            var mapping = MeshBones.Bones.First().BuildRemappingList();
            var numMappings = mapping.Count(x => x.IsUsedByModel);
            if (usedBonesCount != numMappings)
            {
                errorText = "Not all bones mapped. This will not work as you expect and will case problems later!\nOnly do this if your REALLY know what you are doing";
                return false;
            }
            errorText = "";
            return true;
        }
    }

    class FilterHelper
    {
        public static ObservableCollection<AnimatedBone> FilterBoneList(string filterText, bool onlySHowUsedBones, ObservableCollection<AnimatedBone> completeList)
        {
            var output = new ObservableCollection<AnimatedBone>();
            FilterBoneListRecursive(filterText, onlySHowUsedBones, completeList, output);
            return completeList;
        }

        static void FilterBoneListRecursive(string filterText, bool onlySHowUsedBones, ObservableCollection<AnimatedBone> completeList, ObservableCollection<AnimatedBone> output)
        {
            foreach (var item in completeList)
            {
                bool isVisible = IsBoneVisibleInFilter(item, onlySHowUsedBones, filterText, true);
                item.IsVisible = isVisible;
                if (isVisible)
                    FilterBoneListRecursive(filterText, onlySHowUsedBones, item.Children, item.Children);
            }
        }

        static bool IsBoneVisibleInFilter(AnimatedBone bone, bool onlySHowUsedBones, string filterText, bool checkChildren)
        {
            var contains = bone.Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) != -1;
            if (onlySHowUsedBones)
            {
                if (contains && bone.IsUsedByCurrentModel)
                    return contains;
            }
            else
            {
                if (contains)
                    return contains;
            }

            if (checkChildren)
            {
                foreach (var child in bone.Children)
                {
                    var res = IsBoneVisibleInFilter(child, onlySHowUsedBones, filterText, checkChildren);
                    if (res == true)
                        return true;
                }
            }

            return false;
        }
    }
}
