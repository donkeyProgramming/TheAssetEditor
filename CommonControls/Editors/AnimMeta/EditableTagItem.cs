using Common;
using CommonControls.Common;
using CommonControls.MathViews;
using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Xna.Framework;
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

        public virtual byte[] GetByteValue()
        {
            return _parser.Encode(ValueAsString, out _);
        }
    }


    public class OrientationEditableTagItem : EditableTagItem
    {
        public Vector3ViewModel Value { get; set; } = new Vector3ViewModel(0);

        Vector4Parser _parser;

        public OrientationEditableTagItem(Vector4Parser parser, byte[] value) : base(parser, value)
        {
            _parser = parser;

            if (parser.TryDecodeValue(value, 0, out var vector4, out var _, out var err))
            {
                Quaternion q = new Quaternion(vector4);
                Value.Set(MathUtil.ToAxisAngleDegrees(q));
            }
            else
            {
                IsValid = false;
            }
        }

        public override byte[] GetByteValue()
        {
            var vector3 = Value.GetAsVector3();


            var value = MathUtil.FromAxisAngleDegrees(vector3);
            value.Normalize();


            var test = MathUtil.ToAxisAngleDegrees(value);


            var bytes = _parser.EncodeValue(value.ToVector4(), out var err);
            return bytes;
        }
    }
}

