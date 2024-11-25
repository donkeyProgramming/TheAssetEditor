using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using CommonControls.BaseDialogs.ErrorListDialog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.CampaignAnimBin
{
    class CampaignAnimBinToXmlConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            try
            {
                var bin = CampaignAnimationBinLoader.Load(new ByteChunk(bytes));
                var xmlserializer = new XmlSerializer(typeof(CampaignAnimationBin));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true }))
                {
                    xmlserializer.Serialize(writer, bin);
                    var str = stringWriter.ToString();
                    return str;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        void DisplayValidateDialog(ErrorList list)
        {
            if (list == null || !list.HasData) return;
            ErrorListWindow.ShowDialog("Potential problems", list);
        }
        private bool ValidateAnimationData(CampaignAnimationBin campaignAnimation, IPackFileService pfs, string path)
        {
            var IsSkeletonExist = pfs.FindFile($"animations/skeletons/{campaignAnimation.SkeletonName}.anim") != null;

            var fileName = Path.GetFileName(path).Substring(0, Path.GetFileName(path).Length - 4);
            var IsNameOk = campaignAnimation.Reference == fileName;
            var IsNotEmpty = campaignAnimation.Status.Count > 0;
            var IsItemStatus_NormalFound = false;
            var IsItemGlobalFound = false;
            var IsDockDefined = false;
            var AreAllFilesOk = true;

            var CollectionsNotFoundAnims = new Dictionary<string, List<string>>();
            var CollectionsNotFoundMeta = new Dictionary<string, List<string>>();
            var CollectionsNotFoundSound = new Dictionary<string, List<string>>();

            foreach (var item in campaignAnimation.Status)
            {
                if (item.Name == "status_normal" && !IsItemStatus_NormalFound) IsItemStatus_NormalFound = true;
                if (item.Name == "global") IsItemGlobalFound = true;

                foreach (var item2 in item.Transitions)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("Transition")) CollectionsNotFoundAnims["Transition"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Transition")) CollectionsNotFoundMeta["Transition"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Transition")) CollectionsNotFoundSound["Transition"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Transition"].Add(item2.Animation);

                    var IsMetaFound = item2.AnimationMeta == "" || pfs.FindFile(item2.AnimationMeta) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Transition"].Add(item2.AnimationMeta);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Transition"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.Idle)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("Idle")) CollectionsNotFoundAnims["Idle"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Idle")) CollectionsNotFoundMeta["Idle"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Idle")) CollectionsNotFoundSound["Idle"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Idle"].Add(item2.Animation);

                    var IsMetaFound = item2.MetaFile == "" || pfs.FindFile(item2.MetaFile) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Idle"].Add(item2.MetaFile);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Idle"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.Docks)
                {
                    if (item.Name == "global") IsDockDefined = true;

                    if (!CollectionsNotFoundAnims.ContainsKey("Docks")) CollectionsNotFoundAnims["Docks"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Docks")) CollectionsNotFoundMeta["Docks"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Docks")) CollectionsNotFoundSound["Docks"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Docks"].Add(item2.Animation);

                    var IsMetaFound = item2.AnimationMeta == "" || item2.AnimationMeta == "global" || pfs.FindFile(item2.AnimationMeta) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Docks"].Add(item2.AnimationMeta);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Docks"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.Selection)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("Selection")) CollectionsNotFoundAnims["Selection"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Selection")) CollectionsNotFoundMeta["Selection"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Selection")) CollectionsNotFoundSound["Selection"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Selection"].Add(item2.Animation);

                    var IsMetaFound = item2.MetaFile == "" || pfs.FindFile(item2.MetaFile) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Selection"].Add(item2.MetaFile);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Selection"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.Action)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("Action")) CollectionsNotFoundAnims["Action"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Action")) CollectionsNotFoundMeta["Action"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Action")) CollectionsNotFoundSound["Action"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Action"].Add(item2.Animation);

                    var IsMetaFound = item2.Meta == "" || pfs.FindFile(item2.Meta) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Action"].Add(item2.Meta);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Action"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.Locomotion)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("Locomotion")) CollectionsNotFoundAnims["Locomotion"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Locomotion")) CollectionsNotFoundMeta["Locomotion"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Locomotion")) CollectionsNotFoundSound["Locomotion"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Locomotion"].Add(item2.Animation);

                    var IsMetaFound = item2.AnimationMeta == "" || item2.AnimationMeta == "global" || pfs.FindFile(item2.AnimationMeta) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Locomotion"].Add(item2.AnimationMeta);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Locomotion"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.PersitantMetaData)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("PersitantMetaData")) CollectionsNotFoundAnims["PersitantMetaData"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("PersitantMetaData")) CollectionsNotFoundMeta["PersitantMetaData"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("PersitantMetaData")) CollectionsNotFoundSound["PersitantMetaData"] = new List<string>();

                    var IsAnimFound = item2.Animation == "" || pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["PersitantMetaData"].Add(item2.Animation);

                    var IsMetaFound = item2.AnimationMeta == "" || item2.AnimationMeta == "global" || pfs.FindFile(item2.AnimationMeta) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["PersitantMetaData"].Add(item2.AnimationMeta);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["PersitantMetaData"].Add(item2.SoundMeta);
                }

                foreach (var item2 in item.Poses)
                {
                    if (!CollectionsNotFoundAnims.ContainsKey("Poses")) CollectionsNotFoundAnims["Poses"] = new List<string>();
                    if (!CollectionsNotFoundMeta.ContainsKey("Poses")) CollectionsNotFoundMeta["Poses"] = new List<string>();
                    if (!CollectionsNotFoundSound.ContainsKey("Poses")) CollectionsNotFoundSound["Poses"] = new List<string>();

                    var IsAnimFound = pfs.FindFile(item2.Animation) != null;
                    if (!IsAnimFound) CollectionsNotFoundAnims["Poses"].Add(item2.Animation);

                    var IsMetaFound = item2.AnimationMeta == "" || item2.AnimationMeta == "global" || pfs.FindFile(item2.AnimationMeta) != null;
                    if (!IsMetaFound) CollectionsNotFoundMeta["Poses"].Add(item2.AnimationMeta);

                    var IsSoundFound = item2.SoundMeta == "" || pfs.FindFile(item2.SoundMeta) != null;
                    if (!IsSoundFound) CollectionsNotFoundSound["Poses"].Add(item2.SoundMeta);
                }
            }

            foreach (var item in CollectionsNotFoundAnims)
            {
                AreAllFilesOk &= item.Value.Count == 0;
            }
            foreach (var item in CollectionsNotFoundMeta)
            {
                AreAllFilesOk &= item.Value.Count == 0;
            }
            foreach (var item in CollectionsNotFoundSound)
            {
                AreAllFilesOk &= item.Value.Count == 0;
            }

            if (IsNameOk &&
               IsDockDefined &&
               IsItemStatus_NormalFound &&
               IsItemGlobalFound &&
               IsSkeletonExist &&
               IsNotEmpty &&
               AreAllFilesOk &&
               IsSkeletonExist)
            {
                return true;
            }

            var errorItem = new ErrorList();

            if (!IsNotEmpty)
            {
                errorItem.Error("no animation", "there's no animation data in this bin");
            }

            if (!AreAllFilesOk)
            {
                foreach (var item in CollectionsNotFoundAnims)
                {
                    foreach (var item2 in item.Value)
                    {
                        errorItem.Error($"animation not found for category {item.Key}", $"cannot find {item2}");
                    }
                }
                foreach (var item in CollectionsNotFoundMeta)
                {
                    foreach (var item2 in item.Value)
                    {
                        errorItem.Error($"animation meta not found for category {item.Key}", $"cannot find {item2}");
                    }
                }
                foreach (var item in CollectionsNotFoundSound)
                {
                    foreach (var item2 in item.Value)
                    {
                        errorItem.Error($"animation sound meta not found for category {item.Key}", $"cannot find {item2}");
                    }
                }
            }

            if (!IsSkeletonExist)
            {
                errorItem.Error("skeleton not found", $"this skeleton is not defined {campaignAnimation.SkeletonName}");
            }

            if (!IsDockDefined)
            {
                errorItem.Warning("dock animation", "dock animations appear to be never defined");
            }

            if (!IsItemStatus_NormalFound)
            {
                errorItem.Warning("t-pose ahead", "your character will tpose in campaign map");
            }
            if (!IsNameOk)
            {
                errorItem.Error("reference does not match with bin filename", $"your reference is {campaignAnimation.Reference} it should've been {fileName}");
            }

            ErrorListWindow.ShowDialog("Potential problems", errorItem, false);

            return false;
        }
        public byte[] ToBytes(string text, string filePath, IPackFileService pfs, out ITextConverter.SaveError error)
        {
            try
            {
                var xmlserializer = new XmlSerializer(typeof(CampaignAnimationBin));
                using var stringReader = new StringReader(text);
                var reader = XmlReader.Create(stringReader);

                var errorHandler = new XmlSerializationErrorHandler();

                var obj = xmlserializer.Deserialize(reader, errorHandler.EventHandler);
                var typedObject = obj as CampaignAnimationBin;
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                var bytes = CampaignAnimationBinLoader.Write(typedObject, fileName);

                ValidateAnimationData(typedObject, pfs, filePath);
                if (errorHandler.Error != null)
                {
                    error = errorHandler.Error;
                    return null;
                }
                error = null;
                return bytes;

            }
            catch (Exception e)
            {
                var inner = ExceptionHelper.GetInnerMostException(e);
                if (inner is XmlException xmlException)
                    error = new ITextConverter.SaveError() { Text = xmlException.Message, ErrorLineNumber = xmlException.LineNumber, ErrorPosition = xmlException.LinePosition, ErrorLength = 0 };
                else
                    error = new ITextConverter.SaveError() { Text = e.Message };

                return null;
            }
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => false;
    }
}
