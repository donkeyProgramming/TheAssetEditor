using System.Text;
using System.Windows;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.AnimationFilePreviewEditor
{
    class AnimFileToTextConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            try
            {
                var animFile = AnimationFile.Create(new ByteChunk(bytes));
                var output = new StringBuilder();

                output.AppendLine($"Header:");
                output.AppendLine($"\t Version:{animFile.Header.Version}");
                output.AppendLine($"\t Unknown0_alwaysOne:{animFile.Header.Unknown0_alwaysOne}");
                output.AppendLine($"\t FrameRate:{animFile.Header.FrameRate}");
                output.AppendLine($"\t SkeletonName:{animFile.Header.SkeletonName}");
                output.AppendLine($"\t Unknown_V8_AlwaysSix:{animFile.Header.UnknownValue_v8}");
                output.AppendLine($"\t FlagCount:{animFile.Header.FlagCount}");
                for (int i = 0; i < animFile.Header.FlagVariables.Count; i++)
                    output.AppendLine($"\t\t FlagValue[{i}]:{animFile.Header.FlagVariables[i]}");

                output.AppendLine($"\t AnimationTotalPlayTimeInSec:{animFile.Header.AnimationTotalPlayTimeInSec}");

                output.AppendLine("");
                output.AppendLine($"Bones:{animFile.Bones.Length}");
                for (int i = 0; i < animFile.Bones.Length; i++)
                    output.AppendLine($"\t [{i}]:{animFile.Bones[i].Name} id:{animFile.Bones[i].Id} parentId:{animFile.Bones[i].ParentId}");

                output.AppendLine($"NumAnimationParts:{animFile.AnimationParts.Count}");
                int partIndex = 1;
                foreach (var part in animFile.AnimationParts)
                {
                    output.AppendLine("");
                    output.AppendLine($"\t Animation Index:{partIndex++}");
                    output.AppendLine($"\t TranslationMappings:{part.TranslationMappings.Count}");
                    for (int i = 0; i < part.TranslationMappings.Count; i++)
                        output.AppendLine($"\t\t [{i}]:{part.TranslationMappings[i]}");

                    output.AppendLine("");
                    output.AppendLine($"\t RotationMappings:{part.RotationMappings.Count}");
                    for (int i = 0; i < part.RotationMappings.Count; i++)
                        output.AppendLine($"\t\t [{i}]:{part.RotationMappings[i]}");

                    output.AppendLine("");
                    output.AppendLine($"\t Static frame:");
                    if (part.StaticFrame != null)
                        PrintFrame(output, 2, part.StaticFrame);

                    output.AppendLine("");
                    output.AppendLine($"\t Dynamic frames:{part.DynamicFrames.Count}");
                    for (int i = 0; i < part.DynamicFrames.Count; i++)
                    {
                        output.AppendLine($"\t\t Frame:{i}");
                        PrintFrame(output, 3, part.DynamicFrames[i]);
                    }
                }

                return output.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        void PrintFrame(StringBuilder output, int indentation, AnimationFile.Frame frame)
        {
            var indent = new string('\t', indentation);

            output.AppendLine($"{indent} Transforms:{frame.Transforms.Count}");
            for (int i = 0; i < frame.Transforms.Count; i++)
                output.AppendLine($"{indent} \t[{i}] {frame.Transforms[i]}");

            output.AppendLine($"{indent} Quaternion:{frame.Quaternion.Count}");
            for (int i = 0; i < frame.Quaternion.Count; i++)
                output.AppendLine($"{indent} \t[{i}] {frame.Quaternion[i]}");
        }

        public byte[] ToBytes(string text, string filePath, IPackFileService pfs, out ITextConverter.SaveError error)
        {
            error = new ITextConverter.SaveError() { Text = "This file type can not be saved" };
            return null;
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "JSON";
        public bool CanSaveOnError() => false;
    }
}
