using System.IO;
using CommonControls.BaseDialogs;
using MessageBox = System.Windows.MessageBox;

namespace Shared.Ui.BaseDialogs.PackFileBrowser
{
    /// <summary>
    /// Shows simple value nntry dialog, for file/folder names
    /// Shows error boxes on invalid input, asks the user to retry, and shows the dialog again
    /// TODO: Make neater solution? Edit-in-place; make own simple value editing dialog, with checking
    /// </summary>
    public class EditFileNameDialog
    {
        static public string ShowDialog(TreeNode parentNode, string currentValue)
        {
            var inputCorrect = false;
            var dialogValue = currentValue;
            var newFileName = "";

            while (!inputCorrect)
            {
                inputCorrect = true;
                var window = new TextInputWindow("Create folder", dialogValue, true);
                dialogValue = window.TextValue; // store for next dialog instance (if ínput is invalud)

                if (window.ShowDialog() == false)
                    return ""; // exit is "cancel" pressed

                inputCorrect = inputCorrect && IsStringProper(window.TextValue);
                
                newFileName = window.TextValue.ToLower().Trim();

                inputCorrect = inputCorrect && IsFileNameUniqueInFolder(parentNode, newFileName);
                inputCorrect = inputCorrect && AreStringCharsValidForFileName(newFileName);
                
            }

            return newFileName;
        }

        private static bool IsStringProper(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("Folder name can not be empty! Please Try Again.\nPlease Try Again.");
                return false;
            }
            return true;
        }

        private static bool IsFileNameUniqueInFolder(TreeNode parentNode, string newFileName)
        {
            foreach (var node in parentNode.Children)
            {
                if (node.Name == newFileName)
                {
                    MessageBox.Show($"Folder with name '{node.Name}' already exists in this folder!\nPlease Try Again.");
                    return false;
                }
            }

            return true;
        }

        static private bool AreStringCharsValidForFileName(string fileNameToCheck)
        {
            var listOfInvalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in fileNameToCheck)
            {
                foreach (var invalidChar in listOfInvalidChars)
                {
                    if (c == invalidChar)
                    {
                        MessageBox.Show($"Folder name contains invalid character: {c}. \nPlease Try Again.");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
