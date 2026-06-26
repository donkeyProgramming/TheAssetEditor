using System.Windows;
using CommonControls.BaseDialogs;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.AnimationPack;


namespace Editors.AnimationFragmentEditor.AnimationPack.Commands
{
    public class CreateExampleAnimationDbCommand : IAeCommand
    {
        private readonly IFileSaveService _saveHelper;
        private readonly IPackFileService _pfs;

        void IAeCommand.Execute() => throw new NotSupportedException("Use specific methods instead.");

        public CreateExampleAnimationDbCommand(IFileSaveService saveHelper, IPackFileService pfs)
        {
            _saveHelper = saveHelper;
            _pfs = pfs;
        }

        public PackFile? CreateAnimationDbWarhammer3()
        {
            var window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
                return CreateAnimationDbWarhammer3(window.TextValue);
            return null;
        }

        static string GenerateWh3AnimPackName(string name)
        {
            var fileName = SaveUtility.EnsureEnding(name, ".animpack");
            var filePath = @"animations/database/battle/bin/" + fileName;
            return filePath;
        }

        PackFile? CreateAnimationDbWarhammer3(string name)
        {
            var filePath = GenerateWh3AnimPackName(name);

            if (!SaveUtility.IsFilenameUnique(_pfs, filePath))
            {
                MessageBox.Show("Filename is not unique");
                return null;
            }

            var animPack = new AnimationPackFileDatabase("Placeholder");
            return _saveHelper.Save(filePath, AnimationPackSerializer.ConvertToBytes(animPack), false);
        }

        public void CreateAnimationDb3k()
        {
            var window = new TextInputWindow("New AnimPack name", "");
            if (window.ShowDialog() == true)
            {
                var fileName = SaveUtility.EnsureEnding(window.TextValue, ".animpack");
                var filePath = @"animations/database/battle/bin/" + fileName;
                if (!SaveUtility.IsFilenameUnique(_pfs, filePath))
                {
                    MessageBox.Show("Filename is not unique");
                    return;
                }

                // Create dummy data
                var animPack = new AnimationPackFileDatabase("Placeholder");
                _saveHelper.Save(filePath, AnimationPackSerializer.ConvertToBytes(animPack), false);
            }
        }


     
    }
}
