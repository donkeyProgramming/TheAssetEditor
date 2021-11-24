using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.ErrorListDialog;
using CommonControls.Services;
using FileTypes.AnimationPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using static CommonControls.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimationFragmentToXmlConverter : ITextConverter
    {
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public AnimationFragmentToXmlConverter(SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
        }

        public string GetText(byte[] bytes)
        {
            try
            {
                var frgFile = new FileTypes.AnimationPack.AnimationFragment("", new Filetypes.ByteParsing.ByteChunk(bytes));
                var xmlFrg = ConvertAnimationFragmentFileToXmlFragment(frgFile);

                var xmlserializer = new XmlSerializer(typeof(Animation));
                var stringWriter = new StringWriter();
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true }))
                {
                    xmlserializer.Serialize(writer, xmlFrg, ns);
                    var str = stringWriter.ToString();

                    var skeletonName = frgFile.Skeletons.Values.FirstOrDefault();

                    str = str.Replace("</AnimationFragmentEntry>", "</AnimationFragmentEntry>\n");
                    str = str.Replace($"skeleton=\"{skeletonName}\">", $"skeleton=\"{skeletonName}\">\n");
                    return str;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        public byte[] ToBytes(string text, string filePath, PackFileService pfs, out ITextConverter.SaveError error)
        {
            var xmlserializer = new XmlSerializer(typeof(Animation));
            using var sr = new StringReader(text);
            using var reader = XmlReader.Create(sr);

            try
            {
                var errorHandler = new XmlSerializationErrorHandler();
                var obj = xmlserializer.Deserialize(reader, errorHandler.EventHandler) as Animation;
                
                if (errorHandler.Error != null)
                {
                    error = errorHandler.Error;
                    return null;
                }

                error = Validate(obj, text, pfs);
                if (error != null)
                    return null;

                var fragmentFile = ConvertXmlAnimationToAnimationFragmentFile(obj, filePath);
                return fragmentFile.ToByteArray();
            }
            catch (Exception e)
            {
                var inner = ExceptionHelper.GetInnerMostException(e);
                if (inner is XmlException xmlException)
                    error = new ITextConverter.SaveError() { Text = xmlException.Message, ErrorLineNumber = xmlException.LineNumber, ErrorPosition = xmlException.LinePosition, ErrorLength = 0 };
                else if (inner != null)
                    error = new ITextConverter.SaveError() { Text = e.Message + " - " + inner.Message, ErrorLineNumber = 1};
                else
                    error = new ITextConverter.SaveError() { Text = e.Message, ErrorLineNumber = 1 };

                return null;
            }
           
        }

        ITextConverter.SaveError Validate(Animation xmlAnimation, string text, PackFileService pfs)
        {
            if (string.IsNullOrWhiteSpace(xmlAnimation.Skeleton))
                return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Missing skeleton item on root" };

            var lastIndex = 0;
          
            for(int i = 0; i < xmlAnimation.AnimationFragmentEntry.Count; i++)
            {
                var item = xmlAnimation.AnimationFragmentEntry[i];
                lastIndex = text.IndexOf("<AnimationFragmentEntry", lastIndex + 1, StringComparison.InvariantCultureIgnoreCase);

                if (item.Slot == null)
                    return GenerateError(text, lastIndex, "No slot provided");

                var slot = AnimationSlotTypeHelper.GetfromValue(item.Slot);
                if (slot == null)
                    return GenerateError(text, lastIndex, $"{item.Slot} is an invalid animation slot.");

                if (item.File == null)
                    return GenerateError(text, lastIndex, "No file item provided");

                if (item.Meta == null)
                    return GenerateError(text, lastIndex, "No meta item provided");

                if (item.Sound == null)
                    return GenerateError(text, lastIndex, "No sound item provided");

                if (item.BlendInTime == null)
                    return GenerateError(text, lastIndex, "No BlendInTime item provided");

                if (item.SelectionWeight == null)
                    return GenerateError(text, lastIndex, "No SelectionWeight item provided");

                if (item.Unknown == null)
                    return GenerateError(text, lastIndex, "No Unknown item provided");

                if(ValidateBoolArray(item.Unknown) == false)
                    return GenerateError(text, lastIndex, "Unknown bool array contains invalid values. Should contain 6 true/false values");

                if (item.Unknown == null)
                    return GenerateError(text, lastIndex, "No WeaponBone item provided");

                if (ValidateBoolArray(item.WeaponBone) == false)
                    return GenerateError(text, lastIndex, "WeaponBone bool array contains invalid values. Should contain 6 true/false values");
            }

            var errorList = new ErrorList();
            if(_skeletonAnimationLookUpHelper.GetSkeletonFileFromName(pfs, xmlAnimation.Skeleton) == null)
                errorList.Warning("Root", $"Skeleton {xmlAnimation.Skeleton} is not found");

            foreach (var item in xmlAnimation.AnimationFragmentEntry)
            {
                if (string.IsNullOrWhiteSpace(item.File.Value))
                    errorList.Warning(item.Slot, "Item does not have an animation");

                if(pfs.FindFile(item.File.Value) == null)
                    errorList.Warning(item.Slot, $"Animation {item.File.Value} is not found");

                if (item.Meta.Value != "" && pfs.FindFile(item.Meta.Value) == null)
                    errorList.Warning(item.Slot, $"Meta {item.Meta.Value} is not found");

                if (item.Sound.Value != "" && pfs.FindFile(item.Sound.Value) == null)
                    errorList.Warning(item.Slot, $"Sound {item.Sound.Value} is not found");
            }

            if(errorList.Errors.Count != 0)
                ErrorListWindow.ShowDialog("Errors", errorList, false);

            return null;
        }

        ITextConverter.SaveError GenerateError(string wholeText, int lastIndex, string errorMessage)
        {
            var array = wholeText.ToCharArray();
            var lineCount = 0;
            for (int strIndex = 0; strIndex < lastIndex; strIndex++)
            {
                if (array[strIndex] == '\n')
                    lineCount++;
            }

            return new ITextConverter.SaveError() { ErrorLength = 40, ErrorPosition = 0, ErrorLineNumber = lineCount, Text = errorMessage };
        }

        bool ValidateBoolArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split(",");
            if (parts.Length != 6)
                return false;

            for (int i = 0; i < 6; i++)
            {
                var str = parts[i].Trim();
                if (bool.TryParse(str, out _) == false)
                    return false;
            }
            return true;
        }


        Animation ConvertAnimationFragmentFileToXmlFragment(FileTypes.AnimationPack.AnimationFragment fragmentFile)
        {
            var outputBin = new Animation();
            outputBin.AnimationFragmentEntry = new List<AnimationEntry>();
            outputBin.Skeleton = fragmentFile.Skeletons.Values.FirstOrDefault();

            foreach (var item in fragmentFile.Fragments)
            {
                var entry = new AnimationEntry();
                entry.Slot = item.Slot.Value;
                entry.File = new File() { Value = item.AnimationFile };
                entry.Meta = new Meta() { Value = item.MetaDataFile };
                entry.Sound = new Sound() { Value = item.SoundMetaDataFile };
                entry.BlendInTime = new BlendInTime() { Value = item.BlendInTime };
                entry.SelectionWeight = new SelectionWeight() { Value = item.SelectionWeight };

                var unknown0BitArray = new BitArray(new int[] { item.Unknown0 });
                var unknown0Bits = new bool[unknown0BitArray.Count];
                unknown0BitArray.CopyTo(unknown0Bits, 0);

                string[] unknown0StrArray = new string[6];
                for (int i = 0; i < 6; i++)
                    unknown0StrArray[i] = unknown0Bits[i].ToString();

                entry.Unknown = string.Join(", ", unknown0StrArray);


                var unknown1BitArray = new BitArray(new int[] { item.Unknown1 });
                var unknown1Bits = new bool[unknown1BitArray.Count];
                unknown1BitArray.CopyTo(unknown1Bits, 0);

                string[] unknown1StrArray = new string[6];
                for (int i = 0; i < 6; i++)
                    unknown1StrArray[i] = unknown1Bits[i].ToString();

                entry.WeaponBone = string.Join(", ", unknown1StrArray);

                outputBin.AnimationFragmentEntry.Add(entry);
            }

            return outputBin;
        }

        FileTypes.AnimationPack.AnimationFragment ConvertXmlAnimationToAnimationFragmentFile(Animation animation, string fileName)
        {
            var output = new FileTypes.AnimationPack.AnimationFragment(fileName);
            output.Skeletons = new FileTypes.AnimationPack.AnimationFragment.StringArrayTable(animation.Skeleton, animation.Skeleton);

            foreach (var item in animation.AnimationFragmentEntry)
            {
                var entry = new AnimationFragmentEntry()
                {
                    AnimationFile = item.File.Value,
                    MetaDataFile = item.Meta.Value,
                    SoundMetaDataFile = item.Sound.Value,
                    Comment = "",
                    BlendInTime = item.BlendInTime.Value,
                    Ignore = false,
                    SelectionWeight = item.SelectionWeight.Value,
                    Slot = AnimationSlotTypeHelper.GetfromValue(item.Slot),
                    Skeleton = animation.Skeleton,
                };

                var unknown0Flags = item.Unknown.Split(",");
                for (int i = 0; i < 6; i++)
                    entry.SetUnknown0Flag(i, bool.Parse(unknown0Flags[i]));

                var unknown1Flags = item.WeaponBone.Split(",");
                for (int i = 0; i < 6; i++)
                    entry.SetUnknown1Flag(i, bool.Parse(unknown1Flags[i]));

                output.Fragments.Add(entry);
            }

            output.UpdateMinAndMaxSlotIds();
            return output;
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => false;


        [XmlRoot(ElementName = "BlendInTime")]
        public class BlendInTime
        {
            [XmlAttribute(AttributeName = "Value")]
            public float Value { get; set; }
        }

        [XmlRoot(ElementName = "SelectionWeight")]
        public class SelectionWeight
        {
            [XmlAttribute(AttributeName = "Value")]
            public float Value { get; set; }
        }


        [XmlRoot(ElementName = "File")]
        public class File
        {
            [XmlAttribute(AttributeName = "Value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "Meta")]
        public class Meta
        {
            [XmlAttribute(AttributeName = "Value")]
            public string Value { get; set; }
        }


        [XmlRoot(ElementName = "Sound")]
        public class Sound
        {
            [XmlAttribute(AttributeName = "Value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "AnimationEntry")]
        public class AnimationEntry
        {
            [XmlElement(ElementName = "File")]
            public File File { get; set; }

            [XmlElement(ElementName = "Meta")]
            public Meta Meta { get; set; }

            [XmlElement(ElementName = "Sound")]
            public Sound Sound { get; set; }


            [XmlElement(ElementName = "BlendInTime")]
            public BlendInTime BlendInTime { get; set; }
            [XmlElement(ElementName = "SelectionWeight")]
            public SelectionWeight SelectionWeight { get; set; }
            [XmlElement(ElementName = "Unknown")]
            public string Unknown { get; set; }
            [XmlElement(ElementName = "WeaponBone")]
            public string WeaponBone { get; set; }
            [XmlAttribute(AttributeName = "Slot")]
            public string Slot { get; set; }
    
           
        }

        [XmlRoot(ElementName = "Animation")]
        public class Animation
        {
            [XmlElement(ElementName = "AnimationFragmentEntry")]
            public List<AnimationEntry> AnimationFragmentEntry { get; set; }
            [XmlAttribute(AttributeName = "skeleton")]
            public string Skeleton { get; set; }
        }
    }
}
