using CommonControls.Common;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.BoneMapping
{
    public class BoneMappingViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<BoneMappingViewModel>();
        RemappedAnimatedBoneConfiguration _configuration;

        public FilterCollection<AnimatedBone> MeshBones { get; set; }
        public FilterCollection<AnimatedBone> ParentModelBones { get; set; }

        public NotifyAttr<string> CurrentConfigPath { get; set; } 

        public ObservableCollection<string> AllConfigPaths { get; set; }

        public NotifyAttr<bool> OnlyShowUsedBones { get; set; }
        public NotifyAttr<string> MeshSkeletonName { get; set; }
        public NotifyAttr<string> ParentSkeletonName { get; set; }


        public BoneMappingViewModel(RemappedAnimatedBoneConfiguration configuration)
        {
            MeshBones = new FilterCollection<AnimatedBone>(null, OnBoneSelected);
            ParentModelBones = new FilterCollection<AnimatedBone>(null, OnParentBoneSelected);
            OnlyShowUsedBones = new NotifyAttr<bool>(true, (x) => MeshBones.RefreshFilter());
            CurrentConfigPath = new NotifyAttr<string>(string.Empty, OnConfigPathChanged);

            CreateFromConfiguration(configuration);
            FindApplicableSettingsFiles();

            MeshBones.SelectedItem = MeshBones.Values.FirstOrDefault();
            MeshBones.SearchFilterExtended += FilterMeshBones;
            MeshBones.RefreshFilter();

            ParentModelBones.SearchFilterExtended += FilterParentBones;
            ParentModelBones.RefreshFilter();
        }

        void FilterMeshBones(FilterCollection<AnimatedBone> value, Regex regex)
        {
            AnimatedBoneHelper.FilterBoneList(regex, OnlyShowUsedBones.Value, value.PossibleValues);
        }

        void FilterParentBones(FilterCollection<AnimatedBone> value, Regex regex)
        {
            AnimatedBoneHelper.FilterBoneList(regex, OnlyShowUsedBones.Value, value.PossibleValues);
        }

        public virtual void ClearBindingSelfAndChildren()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ClearMapping();
        }

        public virtual void AutoMapSelfAndChildrenByName()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnNames(MeshBones.SelectedItem, ParentModelBones.PossibleValues);
        }

        public virtual void AutoMapSelfAndChildrenByHierarchy()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }
            if (ParentModelBones.SelectedItem == null)
            {
                MessageBox.Show("No parent model bone selected - Please select a bone first");
                return;
            }

            BoneMappingHelper.AutomapDirectBoneLinksBasedOnHierarchy(MeshBones.SelectedItem, ParentModelBones.SelectedItem);
        }

        public virtual void ClearBindingSelf()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ClearMapping(false);
        }

        public virtual void CopyMappingToAllChildren()
        {
            if (MeshBones.SelectedItem == null)
            {
                MessageBox.Show("No mesh bone selected - Please select a bone first");
                return;
            }

            MeshBones.SelectedItem.ApplySelfToChildren();
        }

        void CreateFromConfiguration(RemappedAnimatedBoneConfiguration config)
        {
            var boneSelector = _configuration?.SkeletonBoneHighlighter;
            _configuration = config;
            if (_configuration.SkeletonBoneHighlighter == null)
                _configuration.SkeletonBoneHighlighter = boneSelector;

            MeshBones.UpdatePossibleValues(config.MeshBones);
            MeshSkeletonName = new NotifyAttr<string>(config.MeshSkeletonName);

            ParentModelBones.UpdatePossibleValues(config.ParentModelBones);
            ParentSkeletonName = new NotifyAttr<string>(config.ParnetModelSkeletonName);
        }

        private void OnParentBoneSelected(AnimatedBone bone)
        {
            if (bone == null)
                return;

            MeshBones.SelectedItem.MappedBoneIndex.Value = bone.BoneIndex.Value;
            MeshBones.SelectedItem.MappedBoneName.Value = bone.Name.Value;

            OnMappingCreated(MeshBones.SelectedItem.BoneIndex.Value, MeshBones.SelectedItem.MappedBoneIndex.Value);

            if (_configuration.SkeletonBoneHighlighter != null)
            {
                _configuration.SkeletonBoneHighlighter.SelectTargetSkeletonBone(bone.BoneIndex.Value);
            }

        }

        private void OnBoneSelected(AnimatedBone bone)
        {
            if (_configuration.SkeletonBoneHighlighter != null)
            {
                if (bone == null)
                    _configuration.SkeletonBoneHighlighter.SelectSourceSkeletonBone(-1);
                else
                {
                    _configuration.SkeletonBoneHighlighter.SelectSourceSkeletonBone(bone.BoneIndex.Value);
                    if(bone.MappedBoneIndex.Value != -1)
                        _configuration.SkeletonBoneHighlighter.SelectTargetSkeletonBone(bone.MappedBoneIndex.Value);
                }
            }
        }

        

        public void Save()
        {
            var xmlserializer = new XmlSerializer(_configuration.GetType());
            var stringWriter = new StringWriter();
            using (var writer = XmlWriter.Create(stringWriter, settings: new XmlWriterSettings() { Indent = true }))
            {
                xmlserializer.Serialize(writer, _configuration);
                var dataStr = stringWriter.ToString();


                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.OverwritePrompt = true;
                saveFileDialog.InitialDirectory = DirectoryHelper.AnimationIndexMappingDirectory;
                saveFileDialog.DefaultExt = "xml";
                saveFileDialog.Filter = "Mapping files(*.xml)|*.xml;";
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, dataStr);
                    if (AllConfigPaths.Contains(saveFileDialog.FileName) == false)
                        AllConfigPaths.Add(saveFileDialog.FileName);

                    CurrentConfigPath.Value = saveFileDialog.FileName;
                }
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
                    var xmlserializer = new XmlSerializer(_configuration.GetType());
                    using var stringReader = new StringReader(content);
                    var reader = XmlReader.Create(stringReader);
                    var obj = xmlserializer.Deserialize(reader) as RemappedAnimatedBoneConfiguration;

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
            DirectoryHelper.EnsureCreated(DirectoryHelper.AnimationIndexMappingDirectory);
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
            if (filename == CurrentConfigPath.Value)
                return;

            var content = File.ReadAllText(filename);
            var obj = JsonConvert.DeserializeObject<RemappedAnimatedBoneConfiguration>(content);
            CreateFromConfiguration(obj);
        }

        public virtual void OnMappingCreated(int originalBoneIndex, int newBoneIndex)
        { }

        public virtual bool Validate(out string errorText)
        {
            var usedBonesCount = AnimatedBoneHelper.GetUsedBonesCount(MeshBones.PossibleValues.First());
            var mapping = AnimatedBoneHelper.BuildRemappingList(MeshBones.PossibleValues.First());
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

}
