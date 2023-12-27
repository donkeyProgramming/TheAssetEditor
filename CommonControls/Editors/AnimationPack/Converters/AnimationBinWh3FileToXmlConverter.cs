// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationBinWh3FileToXmlConverter : BaseAnimConverter<AnimationBinWh3FileToXmlConverter.XmlFormat, FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3>
    {
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public AnimationBinWh3FileToXmlConverter(SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
        }


        protected override string CleanUpXml(string xmlText)
        {
            xmlText = xmlText.Replace("</BinEntry>", "</BinEntry>\n");
            xmlText = xmlText.Replace("<Bin>", "<Bin>\n");
            xmlText = xmlText.Replace("</GeneralBinData>", "</GeneralBinData>\n");
            return xmlText;
        }

        protected override XmlFormat ConvertBytesToXmlClass(byte[] bytes)
        {

            var binFile = new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3("", bytes);
            var outputBin = new XmlFormat();

            var slotHelper = binFile.TableVersion == 4 ? AnimationSlotTypeHelperWh3.GetInstance() : AnimationSlotTypeHelper3k.GetInstance();
            outputBin.Version = binFile.TableVersion == 4 ? "Wh3" : "ThreeKingdom";

            outputBin.Data = new GeneralBinData()
            {
                TableVersion = binFile.TableVersion,
                TableSubVersion = binFile.TableSubVersion,
                Name = binFile.Name,
                MountBin = binFile.MountBin,
                SkeletonName = binFile.SkeletonName,
                LocomotionGraph = binFile.LocomotionGraph,
                UnknownValue1_RelatedToFlight = binFile.UnknownValue1
            };

            foreach (var animation in binFile.AnimationTableEntries)
            {
                var slotValue = slotHelper.TryGetFromId((int)animation.AnimationId);
                var slotString = $"Not found [id={(int)animation.AnimationId}]";
                if (slotValue != null)
                    slotString = slotValue.Value;

                outputBin.Animations.Add(new Animation()
                {
                    Slot = slotString,
                    BlendId = animation.BlendIn,
                    BlendOut = animation.SelectionWeight,
                    Unk = animation.Unk,
                    WeaponBone = ConvertIntToBoolArray((int)animation.WeaponBools),
                });

                foreach (var animationRef in animation.AnimationRefs)
                {
                    outputBin.Animations.Last().Ref.Add(new Instance()
                    {
                        File = animationRef.AnimationFile,
                        Meta = animationRef.AnimationMetaFile,
                        Sound = animationRef.AnimationSoundMetaFile
                    });
                }
            }

            return outputBin;
        }

        protected override byte[] ConvertToAnimClassBytes(XmlFormat xmlBin, string fileName)
        {
            var binFile = new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3("", null);

            binFile.TableVersion = xmlBin.Data.TableVersion;
            binFile.TableSubVersion = xmlBin.Data.TableSubVersion;
            binFile.Name = xmlBin.Data.Name;
            binFile.MountBin = xmlBin.Data.MountBin;
            binFile.SkeletonName = xmlBin.Data.SkeletonName;
            binFile.LocomotionGraph = xmlBin.Data.LocomotionGraph;
            binFile.UnknownValue1 = xmlBin.Data.UnknownValue1_RelatedToFlight;
            binFile.Unknown = "";

            var slotHelper = binFile.TableVersion == 4 ? AnimationSlotTypeHelperWh3.GetInstance() : AnimationSlotTypeHelper3k.GetInstance();

            foreach (var animationEntry in xmlBin.Animations)
            {
                binFile.AnimationTableEntries.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
                {
                    AnimationId = (uint)slotHelper.GetfromValue(animationEntry.Slot).Id,
                    BlendIn = animationEntry.BlendId,
                    SelectionWeight = animationEntry.BlendOut,
                    WeaponBools = CreateWeaponFlagInt(animationEntry.WeaponBone),
                    Unk = animationEntry.Unk,
                });

                foreach (var animationInstance in animationEntry.Ref)
                {
                    binFile.AnimationTableEntries.Last().AnimationRefs.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = animationInstance.File,
                        AnimationMetaFile = animationInstance.Meta,
                        AnimationSoundMetaFile = animationInstance.Sound
                    });
                }
            }

            return binFile.ToByteArray();
        }

        protected override ITextConverter.SaveError Validate(XmlFormat type, string s, PackFileService pfs, string filepath)
        {
            try
            {

                if (type.Data == null)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Data section of xml missing" };

                if (type.Animations == null)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Animation section of xml missing" };

                if (!(type.Data.TableVersion == 2 || type.Data.TableVersion == 4))
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Incorrect TableVersion - must be 4 (wh3) or 2 (3k)" };

                if (type.Data.TableVersion == 4 && type.Data.TableSubVersion != 3)
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Incorrect TableSubVersion - must be 3 for wh3" };

                if (string.IsNullOrWhiteSpace(type.Data.SkeletonName))
                    return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Missing skeleton item on root" };

                var errorList = new ErrorList();
                if (_skeletonAnimationLookUpHelper.GetSkeletonFileFromName(pfs, type.Data.SkeletonName) == null)
                    errorList.Error("Skeleton", $"Skeleton {type.Data.SkeletonName} is not found");

                if (type.Data.TableVersion == 4)
                {
                    if (string.IsNullOrWhiteSpace(type.Data.LocomotionGraph))
                    {
                        errorList.Warning("LocomotionGraph", $"LocomotionGraph not provided");
                    }
                    else
                    {
                        if (pfs.FindFile(type.Data.LocomotionGraph) == null)
                            errorList.Error("LocomotionGraph", $"LocomotionGraph {type.Data.LocomotionGraph} is not found");
                    }
                }

                var slotHelper = type.Data.TableVersion == 4 ? AnimationSlotTypeHelperWh3.GetInstance() : AnimationSlotTypeHelper3k.GetInstance();

                if (string.IsNullOrWhiteSpace(type.Data.Name))
                {
                    errorList.Error("Name", $"Name can not be empty");
                }
                else
                {
                    var filename = Path.GetFileNameWithoutExtension(filepath).ToLowerInvariant();
                    if (filename != type.Data.Name.ToLowerInvariant())
                        errorList.Error("Name", $"The name of the bin file has to be the same as the provided name. {filename} vs {type.Data.Name}");
                }

                foreach (var animation in type.Animations)
                {
                    var slot = slotHelper.GetfromValue(animation.Slot);
                    if (slot == null)
                        errorList.Error(animation.Slot, $"Not a valid animation slot for game");

                    if (animation.Ref == null || animation.Ref.Count == 0)
                        errorList.Error(animation.Slot, "Slot does not have any animations");

                    foreach (var animationRef in animation.Ref)
                    {
                        if (pfs.FindFile(animationRef.File) == null)
                            errorList.Warning(animation.Slot, $"Animation file {animationRef.File} is not found");
                        else if (!IsAnimFile(animationRef.File, pfs))
                            errorList.Error(animation.Slot, $"Animation file {animationRef.File} does not appears to be a valid animation file");

                        if (pfs.FindFile(animationRef.Meta) == null)
                            errorList.Warning(animation.Slot, $"Meta file {animationRef.Meta} is not found");
                        else if (!IsAnimMetaFile(animationRef.Meta, pfs))
                            errorList.Error(animation.Slot, $"Meta file {animationRef.Meta} does not appear to be a valid meta animation");

                        if (pfs.FindFile(animationRef.Sound) == null)
                            errorList.Warning(animation.Slot, $"Sound file {animationRef.Sound} is not found");
                        else if (!IsSndMetaFile(animationRef.Sound, pfs))
                            errorList.Error(animation.Slot, $"Sound file {animationRef.Sound} does not appear to be a valid meta sound");
                    }
                }

                if (errorList.Errors.Count != 0)
                    ErrorListWindow.ShowDialog("Errors", errorList, false);
            }
            catch (Exception e)
            {
                return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = $"Unknown save error - {e.Message}" };
            }

            return null;
        }

        private bool IsAnimFile(string file, PackFileService pfs)
        {
            bool endsWithAnim = file.EndsWith(".anim");

            var theFile = pfs.FindFile(file);
            var data = theFile.DataSource.ReadData(20);
            bool headerIsReallyAnimFile = (data[0] == 0x06) || (data[0] == 0x07) || (data[0] == 0x08); //check if version is not 6 7 8 (or just check if it's 2)
            return endsWithAnim && headerIsReallyAnimFile;
        }
        private bool IsAnimMetaFile(string file, PackFileService pfs)
        {
            bool endsWithDotMeta = file.EndsWith(".anm.meta") || file.EndsWith(".meta");

            var theFile = pfs.FindFile(file);
            var data = theFile.DataSource.ReadData(20);
            bool headerIsReallyAnimMetaFile = data[0] == 0x02; //check if version is not 6 7 8 (or just check if it's 2)
            return endsWithDotMeta && headerIsReallyAnimMetaFile;
        }

        private bool IsSndMetaFile(string file, PackFileService pfs)
        {
            bool endsWithDotMeta = file.EndsWith(".snd.meta");

            var theFile = pfs.FindFile(file);
            var data = theFile.DataSource.ReadData(20);
            bool headerIsReallyAnimMetaFile = data[0] == 0x02; //check if version is not 6 7 8 (or just check if it's 2)
            return endsWithDotMeta && headerIsReallyAnimMetaFile;
        }


        [XmlRoot(ElementName = "Instance")]
        public class Instance
        {
            [XmlAttribute(AttributeName = "File")]
            public string File { get; set; }
            [XmlAttribute(AttributeName = "Meta")]
            public string Meta { get; set; }
            [XmlAttribute(AttributeName = "Sound")]
            public string Sound { get; set; }
        }

        [XmlRoot(ElementName = "Animation")]
        public class Animation
        {
            [XmlElement(ElementName = "Instance")]
            public List<Instance> Ref { get; set; } = new List<Instance>();
            [XmlAttribute(AttributeName = "Slot")]
            public string Slot { get; set; }
            [XmlAttribute(AttributeName = "BlendId")]
            public float BlendId { get; set; }
            [XmlAttribute(AttributeName = "SelectionWeight")]
            public float BlendOut { get; set; }
            [XmlAttribute(AttributeName = "WeaponBone")]
            public string WeaponBone { get; set; }
            [XmlAttribute(AttributeName = "Unk")]
            public bool Unk { get; set; }
        }


        [XmlRoot(ElementName = "GeneralBinData")]
        public class GeneralBinData
        {
            public uint TableVersion { get; set; }
            public uint TableSubVersion { get; set; }

            public string Name { get; set; }
            public string MountBin { get; set; }
            public string SkeletonName { get; set; }
            public string LocomotionGraph { get; set; }
            public short UnknownValue1_RelatedToFlight { get; set; }
        }

        [XmlRoot(ElementName = "Bin")]
        public class XmlFormat
        {
            [XmlElement(ElementName = "Version")]
            public string Version { get; set; }

            [XmlElement(ElementName = "GeneralBinData")]
            public GeneralBinData Data { get; set; }

            [XmlElement(ElementName = "Animation")]
            public List<Animation> Animations { get; set; } = new List<Animation>();
        }
    }
}
