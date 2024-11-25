using System.Xml;
using System.Xml.Serialization;
using CommonControls.BaseDialogs.ErrorListDialog;
using Editors.Shared.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationBinWh3FileToXmlConverter : BaseAnimConverter<AnimationBinWh3FileToXmlConverter.XmlFormat, AnimationBinWh3>
    {
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private string AnimationPersistanceMetaFileName = "";
        private Dictionary<string, uint> AnimationsVersionFoundInPersistenceMeta = new();

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

            var binFile = new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3("", bytes);
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
            var binFile = new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinWh3("", null);

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
                binFile.AnimationTableEntries.Add(new Shared.GameFormats.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry()
                {
                    AnimationId = (uint)slotHelper.GetfromValue(animationEntry.Slot).Id,
                    BlendIn = animationEntry.BlendId,
                    SelectionWeight = animationEntry.BlendOut,
                    WeaponBools = CreateWeaponFlagInt(animationEntry.WeaponBone),
                    Unk = animationEntry.Unk,
                });

                foreach (var animationInstance in animationEntry.Ref)
                {
                    binFile.AnimationTableEntries.Last().AnimationRefs.Add(new AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = animationInstance.File,
                        AnimationMetaFile = animationInstance.Meta,
                        AnimationSoundMetaFile = animationInstance.Sound
                    });
                }
            }

            return binFile.ToByteArray();
        }


        protected override ITextConverter.SaveError Validate(XmlFormat type, string s, IPackFileService pfs, string filepath)
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
            if (_skeletonAnimationLookUpHelper.GetSkeletonFileFromName(type.Data.SkeletonName) == null)
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
                var filename = System.IO.Path.GetFileNameWithoutExtension(filepath).ToLowerInvariant();
                if (filename != type.Data.Name.ToLowerInvariant())
                    errorList.Error("Name", $"The name of the bin file has to be the same as the provided name. {filename} vs {type.Data.Name}");
            }

            AnimationsVersionFoundInPersistenceMeta.Clear();
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
                    else if (String.IsNullOrWhiteSpace(animationRef.File))
                        errorList.Error(animation.Slot, $"Animation file {animationRef.File} contain whitespace which could trigger a tpose");
                    else
                        ValidateAnimationVersionAgainstPersistenceMeta(animationRef.File, animation.Slot, type.Data.SkeletonName, pfs, errorList);

                    if (pfs.FindFile(animationRef.Meta) == null)
                        errorList.Warning(animation.Slot, $"Meta file {animationRef.Meta} is not found");
                    else if (!IsAnimMetaFile(animationRef.Meta, pfs))
                        errorList.Error(animation.Slot, $"Meta file {animationRef.Meta} does not appear to be a valid meta animation");
                    else
                    {
                        CheckForAnimationVersionsInMeta(animationRef.File, animationRef.Meta, animation.Slot, type.Data.SkeletonName, pfs, errorList);
                    }

                    var mountBin = type.Data.MountBin;
                    CheckForRiderAndHisMountAnimationsVersion(mountBin, AnimPackToValidate, animation.Slot, animationRef.File, pfs, errorList);

                    if (pfs.FindFile(animationRef.Sound) == null)
                        errorList.Warning(animation.Slot, $"Sound file {animationRef.Sound} is not found");
                    else if (!IsSndMetaFile(animationRef.Sound, pfs))
                        errorList.Error(animation.Slot, $"Sound file {animationRef.Sound} does not appear to be a valid meta sound");
                }
            }

            if (errorList.Errors.Count != 0)
                ErrorListWindow.ShowDialog("Errors", errorList, false);

            var hasCriticalError = errorList.Errors.Where(x => x.IsError).Any();
            if(hasCriticalError)
                return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Critical Error found, unable to save" };
            return null;
        }

        private bool IsAnimFile(string file, IPackFileService pfs)
        {
            bool endsWithAnim = file.EndsWith(".anim");

            var theFile = pfs.FindFile(file);
            var data = theFile.DataSource.ReadData(20);
            bool headerIsReallyAnimFile = (data[0] == 0x06) || (data[0] == 0x07) || (data[0] == 0x08); //check if version is not 6 7 8 (or just check if it's 2)
            return endsWithAnim && headerIsReallyAnimFile;
        }
        private bool IsAnimMetaFile(string file, IPackFileService pfs)
        {
            bool endsWithDotMeta = file.EndsWith(".anm.meta") || file.EndsWith(".meta");

            var theFile = pfs.FindFile(file);
            var data = theFile.DataSource.ReadData(20);
            bool headerIsReallyAnimMetaFile = data[0] == 0x02; //check if version is not 6 7 8 (or just check if it's 2)
            return endsWithDotMeta && headerIsReallyAnimMetaFile;
        }

        private bool IsSndMetaFile(string file, IPackFileService pfs)
        {
            bool endsWithDotMeta = file.EndsWith(".snd.meta");

            var theFile = pfs.FindFile(file);
            var data = theFile.DataSource.ReadData(20);
            bool headerIsReallyAnimMetaFile = data[0] == 0x02; //check if version is not 6 7 8 (or just check if it's 2)
            return endsWithDotMeta && headerIsReallyAnimMetaFile;
        }

        private bool CheckForAnimationVersionsInMeta(string mainAnimationFile, string metaFile, string animationSlot, string skeleton, IPackFileService pfs, ErrorList errorList)
        {
            var result = true;


            var theFile = pfs.FindFile(metaFile);
            var data = theFile.DataSource.ReadData();
            var parsed = new MetaDataFileParser().ParseFile(data);

            var mainAnimationHeader = GetAnimationHeader(mainAnimationFile, pfs);
            if (mainAnimationHeader == null)
            {
                errorList.Error(animationSlot, $"Cannot locate animation {mainAnimationFile} while trying to parse meta");
                return false;
            }

            var mainAnimationVersion = mainAnimationHeader.Version;

            var metaItems = parsed.Items;

            foreach (var item in metaItems)
            {
                if (item.DisplayName.Contains("SPLICE"))
                {
                    var splice = (Splice_v11)item;
                    var animPath = splice.Animation;
                    if (animPath == null || animPath == "")
                    {
                        errorList.Warning(animationSlot, $"Animation Meta Splice {metaFile} has no animation defined");
                        result = false;
                        continue;
                    }

                    var animationVersion = GetAnimationHeader(animPath, pfs).Version;

                    if (animationSlot == "PERSISTENT_METADATA_ALIVE") //check for this too, cus this plays throughtout char animation (all of them)
                    {
                        AnimationsVersionFoundInPersistenceMeta[animPath] = animationVersion;
                        AnimationPersistanceMetaFileName = metaFile;
                    }
                    else
                    {
                        if (mainAnimationVersion == 5 && animationVersion == 8) continue; //no idea why this isn't problem in vanilla wh3
                        if (mainAnimationVersion == 8 && animationVersion == 5) continue; //no idea why this isn't problem in vanilla wh3

                        var isTheVersionMatch = animationVersion == mainAnimationVersion;
                        var isMatchedCombat = animationSlot.StartsWith("COMBAT_");
                        var isMountedAnim = animationSlot.StartsWith("RIDER_") && skeleton.Contains("humanoid");

                        if (isMatchedCombat) continue;
                        if (!isMountedAnim) continue;
                        if (isTheVersionMatch) continue;

                        {
                            errorList.Error(animationSlot, $"Animation Meta Splice {metaFile} has different version than in the main animation. File referenced in meta: {animPath} with version {animationVersion} vs in the main animation {mainAnimationFile} with version {mainAnimationVersion}");
                            result = false;
                        }
                    }
                }
                else if (item.DisplayName.Contains("DISABLE_PER") && animationSlot.StartsWith("RIDER_"))
                {
                    errorList.Warning(animationSlot, $"Contains DISABLE_PERSISTENCE which could cause sync rider animation. In metafile: {metaFile}");
                }


            }

            return result;
        }

        private bool ValidateAnimationVersionAgainstPersistenceMeta(string mainAnimationFile, string animationSlot, string skeleton, IPackFileService pfs, ErrorList errorList)
        {
            var versions = AnimationsVersionFoundInPersistenceMeta;
            var result = true;

            var mainAnimation = pfs.FindFile(mainAnimationFile);
            var mainAnimationParsed = AnimationFile.Create(mainAnimation);
            var mainAnimationVersion = mainAnimationParsed.Header.Version;


            foreach (var (animPath, animationVersion) in versions)
            {
                if (mainAnimationVersion == 5 && animationVersion == 8) continue; //no idea why this isn't problem in vanilla wh3
                if (mainAnimationVersion == 8 && animationVersion == 5) continue; //no idea why this isn't problem in vanilla wh3

                var isTheVersionMatch = animationVersion == mainAnimationVersion;
                var isMatchedCombat = animationSlot.StartsWith("COMBAT_");
                var isMountedAnim = animationSlot.StartsWith("RIDER_") && skeleton.Contains("humanoid");

                if (isMatchedCombat) continue;
                if (!isMountedAnim) continue;
                if (isTheVersionMatch) continue;

                {
                    errorList.Error(animationSlot, $"Animation Meta Splice {AnimationPersistanceMetaFileName} has different version than in the main animation. File referenced in meta: {animPath} with version {animationVersion} vs in the main animation {mainAnimationFile} with version {mainAnimationVersion}");
                    result = false;
                }
            }

            return result;
        }

        private bool CheckForRiderAndHisMountAnimationsVersion(string mountBinReference, PackFile animpack, string animationSlot, string animationFile, IPackFileService pfs, ErrorList errorList)
        {
            if (!animationSlot.Contains("RIDER_")) return true;

            var result = true;

            var animPack = AnimationPackSerializer.Load(animpack, pfs);
            var itemNames = animPack.Files.ToList();

            var findMountBinReference = itemNames.Find(x => x.FileName.Contains(mountBinReference));
            if (findMountBinReference == null)
            {
                errorList.Warning(animationSlot, $"Cannot validate referenced {mountBinReference} of this rider, perhaps it's located in outside the current animpak files? or it is defined in another animpack or mod?");
                return false;
            }

            var mountBinBytes = findMountBinReference.ToByteArray();

            var parsedBin = ConvertBytesToXmlClass(mountBinBytes);
            var animations = parsedBin.Animations;

            var riderAnimationSlotWithoutPrefix = animationSlot.Substring(6);
            var mainAnimationToCompareHeader = GetAnimationHeader(animationFile, pfs);
            if(mainAnimationToCompareHeader == null)
            {
                errorList.Warning(animationSlot, $"Cannot validate referenced {mountBinReference} of this rider, perhaps it's located in outside the current animpak files? or it is defined in another animpack or mod?");
                return false;
            }
            var mainAnimationToCompareVersion = mainAnimationToCompareHeader.Version;
            var mainAnimationToData = GetAnimationData(animationFile, pfs);
            var mainAnimationLength = mainAnimationToData.AnimationParts[0].DynamicFrames.Count;
            var mainAnimationTime = mainAnimationToCompareHeader.AnimationTotalPlayTimeInSec;


            foreach (var anim in animations)
            {
                if (anim.Slot != riderAnimationSlotWithoutPrefix) continue;

                var animationInstances = anim.Ref;
                foreach (var animationInstance in animationInstances)
                {
                    var header = GetAnimationHeader(animationInstance.File, pfs);

                    if(header == null)
                    {
                        errorList.Warning(animationSlot, $"Could not locate {animationInstance.File} while trying to validate CheckForRiderAndHisMountAnimationsVersion");
                        continue;
                    }

                    var version = header.Version;
                    var isVersionMatch = version == mainAnimationToCompareVersion;
                    if (!isVersionMatch)
                    {
                        errorList.Error(animationSlot, $"Rider animation version mismatch with mount animation. Mount animation {animationInstance.File} with version {version} vs in the main animation {animationFile} with version {mainAnimationToCompareVersion}");
                        result = false;
                    }

                    var timing = header.AnimationTotalPlayTimeInSec;
                    var data = GetAnimationData(animationInstance.File, pfs);
                    var length = data.AnimationParts[0].DynamicFrames.Count;

                    var isTImingMatch = mainAnimationTime == timing;
                    if (!isTImingMatch)
                    {
                        errorList.Error(animationSlot, $"Rider animation mismatch with mount timing  animation. Mount animation {animationInstance.File} with timing {timing} vs in the main animation {animationFile} with timing {mainAnimationTime}");
                        result = false;
                    }

                    var isLenMatch = mainAnimationLength == length;
                    if (!isLenMatch)
                    {
                        errorList.Error(animationSlot, $"Rider animation mismatch with mount frames animation. Mount animation {animationInstance.File} with length {length} vs in the main animation {animationFile} with timing {mainAnimationLength}");
                        result = false;
                    }
                }
            }

            return result;
        }


        private AnimationFile.AnimationHeader GetAnimationHeader(string path, IPackFileService pfs)
        {
            var mainAnimation = pfs.FindFile(path);
            if (mainAnimation == null) return null;
            var mainAnimationParsed = AnimationFile.Create(mainAnimation);
            return mainAnimationParsed.Header;
        }

        private AnimationFile GetAnimationData(string path, IPackFileService pfs)
        {
            var mainAnimation = pfs.FindFile(path);
            var mainAnimationParsed = AnimationFile.Create(mainAnimation);
            return mainAnimationParsed;
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
