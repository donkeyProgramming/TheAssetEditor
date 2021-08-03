using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.TextEditor
{

    public interface ITextConverter
    {
        string GetText(byte[] bytes);
        byte[] ToBytes(string text);
        bool Validate(string text, out string errorText);
    
    }

    public class DefaultTextConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length))
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                    return reader.ReadToEnd();
            }
        }

        public byte[] ToBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        public bool Validate(string text, out string errorText)
        {
            errorText = string.Empty;
            return true;
        }
    }

    public class TextEditorViewModel<TextConverter> : NotifyPropertyChangedImpl, IEditorViewModel
        where TextConverter : ITextConverter
    {
        public ICommand SaveCommand { get; set; }


        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        string _text;
        public string Text { get => _text; set => SetAndNotify(ref _text, value); }

        IPackFile _packFile;
        PackFileService _pf;

        TextConverter _converter;

        public TextEditorViewModel(PackFileService pf, TextConverter converter)
        {
            _converter = converter;
            _pf = pf;
            SaveCommand = new RelayCommand(() => Save());
        }

        public IPackFile MainFile
        {
            get => _packFile;
            set
            {
                _packFile = value;
                SetCurrentPackFile(_packFile);
            }
        }

        void SetCurrentPackFile(IPackFile packedFile)
        {
            PackFile file = packedFile as PackFile;
            DisplayName = file.Name;

            byte[] data = file.DataSource.ReadData();
            Text = _converter.GetText(data);
        }

        public bool Save()
        {
            var path = _pf.GetFullPath(MainFile as PackFile);

            if (_converter.Validate(Text, out string errorText) == false)
            {
                MessageBox.Show("Unable to save text:\n" + errorText);
                return false;
            }


            var bytes = _converter.ToBytes(Text);
            var res = SaveHelper.Save(_pf, path, MainFile as PackFile, bytes);
            if (res != null)
                MainFile = res;
            return false;
        }

        public void Close()
        {
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }
    }
}
