using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audio.Presentation.Compiler
{
    internal class CompilerViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Compiler");
        public PackFile MainFile { get => _mainPackFile; set { Load(_mainPackFile); } }
        public bool HasUnsavedChanges { get;set; }



        PackFile _mainPackFile;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool Save()
        {
            throw new NotImplementedException();
        }

        void Load(PackFile file) 
        {
            _mainPackFile = file;
        }
    }
}
