using System;
using System.IO;
using CommonControls.BaseDialogs;
using MessageBox = System.Windows.MessageBox;

namespace Shared.Ui.BaseDialogs.PackFileTree
{
    /// <summary>
    /// Shows simple value entry dialog, for file/folder names
    /// Shows error boxes on invalid input, asks the user to retry, and shows the dialog again
    /// TODO: Make neater solution? Edit-in-place; make own simple value editing dialog, with checking
    /// </summary>
    public class EditFileNameDialog
    {
        /// <summary>
        /// Shows dialog for entering a new folder name
        /// </summary>
        /// <param name="parentNode">Parent Node for checking for folders with identical names</param>
        /// <param name="currentValue">The Value of the property to edit</param>
        /// <returns>Edit Name, empty string if user pressed "cancel" </returns>
        static public string ShowDialog(TreeNode parentNode, string currentValue)
        {
            var isInputCorrect = false;
            var dialogTextBoxValue = currentValue;
            var newFileName = "";

            while (!isInputCorrect)
            {
                isInputCorrect = true;
                var window = new TextInputWindow("Create folder", dialogTextBoxValue, true);

                if (window.ShowDialog() == false)
                    return ""; // exit if "cancel" pressed

                dialogTextBoxValue = window.TextValue; // store for next dialog instance (if ínput is invalud)

                isInputCorrect = isInputCorrect && IsStringProper(window.TextValue);

                newFileName = window.TextValue.ToLower().Trim();

                isInputCorrect = isInputCorrect && IsFileNameUniqueInFolder(parentNode, newFileName);

                isInputCorrect = isInputCorrect && AreStringCharsValidForFileName(newFileName);
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
            if (parentNode == null)
                throw new ArgumentNullException(nameof(parentNode), "Parent node can not be null!");

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
