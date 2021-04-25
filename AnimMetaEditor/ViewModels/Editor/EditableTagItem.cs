using Common;
using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows;
using System.Windows.Input;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class EditableTagItem : NotifyPropertyChangedImpl
    {
        byte[] _originalValue;
        IByteParser _parser { get; set; }

        public EditableTagItem(IByteParser parser, byte[] value)
        {
            _originalValue = value;
            _parser = parser;
            isValid = _parser.TryDecode(_originalValue, 0, out _valueAsString, out _, out _);
        }

        string _valueAsString;
        public string ValueAsString { get => _valueAsString; set { SetAndNotify(ref _valueAsString, value); Validate(); } }

        public string FieldName { get; set; }
        public string Description { get; set; }
        public string ValueType { get; set; }
        public ICommand ResetCommand { get; set; }

        bool _isValueValid;
        public bool isValid { get => _isValueValid; set { SetAndNotify(ref _isValueValid, value); } }

        void Validate()
        {
            isValid = _parser.Encode(ValueAsString, out _) != null;
        }

        public byte[] GetByteValue()
        {
            return _parser.Encode(ValueAsString, out _);
        }

        public override string ToString()
        {
            return $"{FieldName} - {ValueAsString} - {isValid}";
        }
    }
}

