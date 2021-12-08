using CommonControls.Common;
using CommonControls.MathViews;
using Filetypes.ByteParsing;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xna.Framework;
using Serilog;
using System.Windows;
using System.Windows.Input;

namespace CommonControls.Editors.AnimMeta
{
    public class EditableTagItem : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<EditableTagItem>();

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
            _logger.Here().Information($"GetByteValue=>{FieldName} {_parser} {ValueAsString}");
            var value = _parser.Encode(ValueAsString, out var error);
            _logger.Here().Information($"GetByteValue Complete=>{value?.Length} {error}");
            return value;
        }
    }


    public class OrientationEditableTagItem : EditableTagItem
    {
        ILogger _logger = Logging.Create<OrientationEditableTagItem>();
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
            _logger.Here().Information($"GetByteValue Orientation=>{FieldName} {_parser} {ValueAsString} {Value}");

            var vector3 = Value.GetAsVector3();
            var value = MathUtil.FromAxisAngleDegrees(vector3);
            value.Normalize();
            _logger.Here().Information($"GetByteValue Orientation=>Vector computed");

            var bytes = _parser.EncodeValue(value.ToVector4(), out var err);
       
            _logger.Here().Information($"GetByteValue Complete=>{bytes?.Length} {err}");

            return bytes;
        }
    }

    public class Vector3EditableTagItem : EditableTagItem
    {
        ILogger _logger = Logging.Create<OrientationEditableTagItem>();
        public Vector3ViewModel Value { get; set; } = new Vector3ViewModel(0);

        Vector3Parser _parser;

        public Vector3EditableTagItem(Vector3Parser parser, byte[] value) : base(parser, value)
        {
            _parser = parser;

            if (parser.TryDecodeValue(value, 0, out var vector3, out var _, out var err))
                Value.Set(vector3);
            else
                IsValid = false;
        }

        public override byte[] GetByteValue()
        {
            _logger.Here().Information($"GetByteValue Vector3=>{FieldName} {_parser} {ValueAsString} {Value}");

            var vector3 = Value.GetAsVector3();
            _logger.Here().Information($"GetByteValue Vector3=>Vector computed");

            var bytes = _parser.EncodeValue(vector3, out var err);
            _logger.Here().Information($"GetByteValue Complete=>{bytes?.Length} {err}");

            return bytes;
        }
    }
}

