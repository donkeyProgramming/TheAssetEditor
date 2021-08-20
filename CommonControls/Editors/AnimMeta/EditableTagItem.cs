using Common;
using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.AnimMeta
{
    public class EditableTagItem : NotifyPropertyChangedImpl
    {
        byte[] _originalByteValue;
        IByteParser _parser { get; set; }

        public EditableTagItem(IByteParser parser, byte[] value)
        {
            _originalByteValue = value;
            _parser = parser;
            IsValid = _parser.TryDecode(_originalByteValue, 0, out _valueAsString, out _, out _);
        }

        string _valueAsString;
        public string ValueAsString { get => _valueAsString; set { SetAndNotify(ref _valueAsString, value); Validate(); } }

        public string FieldName { get; set; }
        public string Description { get; set; }
        public string ValueType { get; set; }

        bool _isValueValid;
        public bool IsValid { get => _isValueValid; set { SetAndNotify(ref _isValueValid, value); } }

        void Validate()
        {
            IsValid = _parser.Encode(ValueAsString, out _) != null;
        }

        public byte[] GetByteValue()
        {
            return _parser.Encode(ValueAsString, out _);
        }
    }
}

