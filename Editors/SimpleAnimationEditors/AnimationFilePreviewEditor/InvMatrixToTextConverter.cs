using System.Text;
using System.Windows;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.AnimationFilePreviewEditor
{
    public class InvMatrixToTextConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            try
            {
                var animFile = AnimInvMatrixFile.Create(new ByteChunk(bytes));

                var output = new StringBuilder();

                output.AppendLine($"Version:{animFile.Version}");
                output.AppendLine($"NumBones:{animFile.MatrixList.Length}");

                for (int i = 0; i < animFile.MatrixList.Length; i++)
                {
                    output.AppendLine($"\t {i}");

                    var m = animFile.MatrixList[i];
                    output.AppendLine($"\t\t [{FloatToString(m.M11)}], [{FloatToString(m.M12)}], [{FloatToString(m.M13)}], [{FloatToString(m.M14)}]");
                    output.AppendLine($"\t\t [{FloatToString(m.M21)}], [{FloatToString(m.M22)}], [{FloatToString(m.M23)}], [{FloatToString(m.M24)}]");
                    output.AppendLine($"\t\t [{FloatToString(m.M31)}], [{FloatToString(m.M32)}], [{FloatToString(m.M33)}], [{FloatToString(m.M34)}]");
                    output.AppendLine($"\t\t [{FloatToString(m.M41)}], [{FloatToString(m.M42)}], [{FloatToString(m.M43)}], [{FloatToString(m.M44)}]");
                }

                return output.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        string FloatToString(float value)
        {
            return string.Format("{0,12:0.00000000}", value);
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
