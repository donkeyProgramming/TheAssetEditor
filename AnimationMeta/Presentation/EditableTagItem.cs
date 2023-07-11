// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Common;
using CommonControls.MathViews;
using Filetypes.ByteParsing;
using Microsoft.Xna.Framework;
using Serilog;

namespace AnimationMeta.Presentation
{
    public class EditableTagItem : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<EditableTagItem>();

        byte[] _originalByteValue;
        IByteParser _parser { get; set; }

        public EditableTagItem(IByteParser parser, object value)
        {
            _parser = parser;
            _valueAsString = value.ToString();
            Validate();
        }

        protected EditableTagItem() { }

        string _valueAsString;
        public string ValueAsString { get => _valueAsString; set { SetAndNotify(ref _valueAsString, value); Validate(); } }

        public string FieldName { get; set; }
        public string Description { get; set; }
        public string ValueType { get; set; }
        public bool IsReadOnly { get; set; } = true;

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

        public OrientationEditableTagItem(Vector4Parser parser, Vector4 value)
        {
            _parser = parser;

            Quaternion q = new Quaternion(value);
            var eulerRotation = MathUtil.QuaternionToEulerDegree(q);

            Value.Set(eulerRotation);
            IsValid = true;

        }

        public override byte[] GetByteValue()
        {
            _logger.Here().Information($"GetByteValue Orientation=>{FieldName} {_parser} {ValueAsString} {Value}");

            var vector3 = Value.GetAsVector3();
            var value = MathUtil.EulerDegreesToQuaternion(vector3);
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

        public Vector3EditableTagItem(Vector3Parser parser, Vector3 value)
        {
            _parser = parser;
            Value.Set(value);
            IsValid = true;
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

