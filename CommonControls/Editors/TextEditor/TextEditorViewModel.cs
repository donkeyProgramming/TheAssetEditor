using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.TextEditor
{
    public interface ITextEditorViewModel
    {
        void SetEditor(ITextEditor theEditor);
    }

    public class TextEditorViewModel<TextConverter> : NotifyPropertyChangedImpl, ITextEditorViewModel, IEditorViewModel
        where TextConverter : ITextConverter
    {
        public ICommand SaveCommand { get; set; }

        string _displayName;
        public string DisplayName { get => _displayName; set => SetAndNotify(ref _displayName, value); }

        string _text;
        public string Text { get => _text; set => SetAndNotify(ref _text, value); }

        PackFile _packFile;
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

        public PackFile MainFile
        {
            get => _packFile;
            set
            {
                _packFile = value;
                SetCurrentPackFile(_packFile);
            }
        }

        void SetCurrentPackFile(PackFile packedFile)
        {
            PackFile file = packedFile ;
            DisplayName = file.Name;

            byte[] data = file.DataSource.ReadData();
            Text = _converter.GetText(data);
        }

        public bool Save()
        {
            var path = _pf.GetFullPath(MainFile );

            var bytes = _converter.ToBytes(Text, path, _pf, out var error);
            if (bytes == null || error != null)
            {
                _textEditor.HightLightText(error.ErrorLineNumber, error.ErrorPosition, error.ErrorLength);

                if (_converter.CanSaveOnError())
                {
                    if (MessageBox.Show(error.Text + "\n\nThis means that the file might not work!\nSave anyway?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        return false;
                }
                else
                {
                    MessageBox.Show(error.Text, "Error");
                    return false;
                }
            }

            var res = SaveHelper.Save(_pf, path, MainFile , bytes);
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


    public class SimpleTextEditorViewModel : NotifyPropertyChangedImpl, ITextEditorViewModel
    {
        ICommand _saveCommand;
        public ICommand SaveCommand { get => _saveCommand; set => SetAndNotify(ref _saveCommand, value); }

        string _text;
        public string Text 
        { 
            get => _text; 
            set 
            { 
                SetAndNotify(ref _text, value);
                _textChanged = true;
            }
        }

        bool _textChanged = false;
        public ITextEditor TextEditor { get; private set; }

        public SimpleTextEditorViewModel()
        {
            SaveCommand = new RelayCommand(() => Save());
        }

        public bool Save() { return true; }

        public void SetEditor(ITextEditor theEditor)
        {
            TextEditor = theEditor;
            TextEditor.ClearUndoStack();
        }   

        public bool HasUnsavedChanges()
        {
            return _textChanged;
        }

        public void ResetChangeLog()
        {
            _textChanged = false;
               TextEditor.ClearUndoStack();
        }
    }
}
