// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;

namespace CommonControls.FileTypes.PackFiles.Models
{
    public interface IDuplicatePackFileResolver
    {
        bool CheckForDuplicates { get; }
        bool KeepDuplicateFile(string fileName);
    }

    public class CaPackDuplicatePackFileResolver : IDuplicatePackFileResolver
    {
        public bool CheckForDuplicates => false;
        public bool KeepDuplicateFile(string fileName) => false;
    }

    public class CustomPackDuplicatePackFileResolver : IDuplicatePackFileResolver
    {
        public bool CheckForDuplicates => true;
        public bool KeepDuplicateFile(string fileName)
        {
            var res = MessageBox.Show($"Multiple files with the name '{fileName}' found.\n Yes = Rename and keep.\nNo = Skip", "DuplicateFile", MessageBoxButton.YesNo);
            return res == MessageBoxResult.Yes;
        }
    }
}
