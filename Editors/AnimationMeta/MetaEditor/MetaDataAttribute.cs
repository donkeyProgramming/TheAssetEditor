using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.ByteParsing.Parsers;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.AnimationMeta.Presentation
{
    public partial class MetaDataAttribute : ObservableObject
    {
        private readonly ILogger _logger = Logging.Create<MetaDataAttribute>();
        private readonly IByteParser _parser;
        protected readonly object _target;
        protected readonly PropertyInfo _property;

        [ObservableProperty] string _valueAsString;
        [ObservableProperty] string _fieldName;
        [ObservableProperty] string _description;
        [ObservableProperty] string _valueType;
        [ObservableProperty] bool _isReadOnly = true;
        [ObservableProperty] bool _isValid = true;

        public MetaDataAttribute(IByteParser parser, object value, object target, PropertyInfo property)
        {
            _parser = parser;
            _target = target;
            _property = property;
            _valueAsString = value.ToString();
            Validate();
        }

        partial void OnValueAsStringChanged(string value) => Validate();

        void Validate()
        {
            IsValid = _parser.Encode(ValueAsString, out _) != null;
            if (IsValid)
            {
                switch (_parser.Type)
                {
                    case DbTypesEnum.String:
                        _property.SetValue(_target, ValueAsString);
                        break;
                    case DbTypesEnum.Integer:
                        _property.SetValue(_target, int.Parse(ValueAsString));
                        break;
                    case DbTypesEnum.Single:
                        _property.SetValue(_target, float.Parse(ValueAsString));
                        break;
                    case DbTypesEnum.Boolean:
                        _property.SetValue(_target, bool.Parse(ValueAsString));
                        break;
                    case DbTypesEnum.Byte:
                        _property.SetValue(_target, byte.Parse(ValueAsString));
                        break;
                    default:
                        var loggingStr = $"Unsupported type {_parser.Type} for property {_property.Name}";
                        _logger.Here().Error(loggingStr);
                        throw new ArgumentException(loggingStr);
                }
            }

        }

        public virtual byte[] GetByteValue()
        {
            _logger.Here().Information($"GetByteValue=>{FieldName} {_parser} {ValueAsString}");
            var value = _parser.Encode(ValueAsString, out var error);
            _logger.Here().Information($"GetByteValue Complete=>{value?.Length} {error}");
            return value;
        }
    }

    public partial class OrientationMetaDataAttribute : MetaDataAttribute
    {
        private readonly ILogger _logger = Logging.Create<OrientationMetaDataAttribute>();
        private readonly Vector4Parser _typedParser;

        [ObservableProperty] Vector3ViewModel _value = new(0, 0, 0);

        public OrientationMetaDataAttribute(Vector4Parser parser, Vector4 value, object target, PropertyInfo property) 
            : base(parser, value, target, property)
        {

            _value = new(0, 0, 0, OnValueChangedCallback);

            _typedParser = parser;

            var q = new Quaternion(value);
            var eulerRotation = MathUtil.QuaternionToEulerDegree(q);

            Value.Set(eulerRotation);
            IsValid = true;
        }

        private void OnValueChangedCallback(Vector3 vector)
        {
            var vector3 = Value.GetAsVector3();
            var value = MathUtil.EulerDegreesToQuaternion(vector3);
            value.Normalize();
            _property.SetValue(_target, value.ToVector4());
        }

        public override byte[] GetByteValue()
        {
            _logger.Here().Information($"GetByteValue Orientation=>{FieldName} {_typedParser} {ValueAsString} {Value}");

            var vector3 = Value.GetAsVector3();
            var value = MathUtil.EulerDegreesToQuaternion(vector3);
            value.Normalize();
            _logger.Here().Information($"GetByteValue Orientation=>Vector computed");

            var bytes = _typedParser.EncodeValue(value.ToVector4(), out var err);

            _logger.Here().Information($"GetByteValue Complete=>{bytes?.Length} {err}");

            return bytes;
        }
    }

    public partial class VectorMetaDataAttribute : MetaDataAttribute
    {
        private readonly ILogger _logger = Logging.Create<VectorMetaDataAttribute>();
        private readonly Vector3Parser _parser;

        [ObservableProperty] Vector3ViewModel _value;

        public VectorMetaDataAttribute(Vector3Parser parser, Vector3 value, object target, PropertyInfo property) : base(parser, value, target, property)
        {
            _value = new(0, 0, 0, OnValueChangedCallback);
            _parser = parser;
            Value.Set(value);
            IsValid = true;
        }


        private void OnValueChangedCallback(Vector3 vector)
        {
            var vector3 = Value.GetAsVector3();
            _property.SetValue(_target, vector3);
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

