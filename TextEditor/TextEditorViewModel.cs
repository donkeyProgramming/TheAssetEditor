using Common;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TextEditor
{
    public class TextEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }


        IPackFile _packFile;
        public IPackFile MainFile
        {
            get => _packFile;
            set
            {
                _packFile = value;
                SetCurrentPackFile(_packFile);
            }
        }


        public string Text { get; set; }

        public bool Save()
        {
            throw new NotImplementedException();
        }



        void SetCurrentPackFile(IPackFile packedFile)
        {
            PackFile file = packedFile as PackFile;
            DisplayName = file.Name;

            byte[] data = file.DataSource.ReadData();
            using (MemoryStream stream = new MemoryStream(data, 0, data.Length))
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                    Text = reader.ReadToEnd();
            }

        }
    }
}
