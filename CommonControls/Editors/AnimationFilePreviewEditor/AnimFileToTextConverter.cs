using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.Animation;
using CommonControls.Services;
using Filetypes.ByteParsing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

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
                output.AppendLine($"\t AnimationFormat:{animFile.Header.AnimationFormat}");
                output.AppendLine($"\t Unknown0_alwaysOne:{animFile.Header.Unknown0_alwaysOne}");
                output.AppendLine($"\t FrameRate:{animFile.Header.FrameRate}");
                output.AppendLine($"\t SkeletonName:{animFile.Header.SkeletonName}");
                output.AppendLine($"\t Unknown1_alwaysZero:{animFile.Header.Unknown1_alwaysZero}");
                output.AppendLine($"\t AnimationTotalPlayTimeInSec:{animFile.Header.AnimationTotalPlayTimeInSec}");

                output.AppendLine("");
                output.AppendLine($"Bones:{animFile.Bones.Length}");
                for(int i = 0; i < animFile.Bones.Length; i++)
                    output.AppendLine($"\t [{i}]:{animFile.Bones[i].Name} id:{animFile.Bones[i].Id} parentId:{animFile.Bones[i].ParentId}");

                output.AppendLine("");
                output.AppendLine($"TranslationMappings:{animFile.TranslationMappings.Count}");
                for (int i = 0; i < animFile.TranslationMappings.Count; i++)
                    output.AppendLine($"\t [{i}]:{animFile.TranslationMappings[i]}");

                output.AppendLine("");
                output.AppendLine($"RotationMappings:{animFile.RotationMappings.Count}");
                for (int i = 0; i < animFile.RotationMappings.Count; i++)
                    output.AppendLine($"\t [{i}]:{animFile.RotationMappings[i]}");

                output.AppendLine("");
                output.AppendLine($"Static frame:");
                if (animFile.StaticFrame != null)
                    PrintFrame(output, 1, animFile.StaticFrame);

                output.AppendLine("");
                output.AppendLine($"Dynamic frames:{animFile.DynamicFrames.Count}");
                for (int i = 0; i < animFile.DynamicFrames.Count; i++)
                {
                    output.AppendLine($"\t Frame:{i}");
                    PrintFrame(output, 2, animFile.DynamicFrames[i]);
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

        public byte[] ToBytes(string text, string filePath, PackFileService pfs, out ITextConverter.SaveError error)
        {
            error = new ITextConverter.SaveError() { Text = "This file type can not be saved" };
            return null;
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "JSON";
        public bool CanSaveOnError() => false;
    }
}
