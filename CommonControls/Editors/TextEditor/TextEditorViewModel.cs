using Common;
using CommonControls.Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.TextEditor
{
    public interface ITextEditorViewModel : IEditorViewModel
    {
        void SetEditor(ITextEditor theEditor);
    }

    public class TextEditorViewModel<TextConverter> : NotifyPropertyChangedImpl, ITextEditorViewModel
        where TextConverter : ITextConverter
    {
        public ICommand SaveCommand { get; set; }

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        string _text;
        public string Text { get => _text; set => SetAndNotify(ref _text, value); }

        IPackFile _packFile;
        PackFileService _pf;

        ITextEditor _textEditor;
        TextConverter _converter;

        public TextEditorViewModel(PackFileService pf, TextConverter converter)
        {
            _converter = converter;
            _pf = pf;
            SaveCommand = new RelayCommand(() => Save());
        }

        public void SetEditor(ITextEditor theEditor)
        {
            _textEditor = theEditor;
            _textEditor.ClearUndoStack();
            _textEditor.ShowLineNumbers(_converter.ShouldShowLineNumbers());
            _textEditor.SetSyntaxHighlighting(_converter.GetSyntaxType());
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

            var bytes = _converter.ToBytes(Text, path, _pf, out var error);
            if (bytes == null || error != null)
            {
                MessageBox.Show(error.Text, "Error");
                _textEditor.HightLightText(error.ErrorLineNumber, error.ErrorPosition, error.ErrorLength);
                return false;
            }

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
