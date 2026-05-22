using System.Reflection;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xna.Framework;
using Serilog;
using Shared.ByteParsing.Parsers;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.AnimationMeta.Presentation
{
    public record MetaDataAttributeChangedEvent();
    public record SelecteMetaDataAttributeChangedEvent();

    public partial class AttributeViewModel : ObservableObject
    {
        private readonly ILogger _logger = Logging.Create<AttributeViewModel>();
        private readonly IByteParser _parser;
        protected readonly object _target;
        protected readonly PropertyInfo _property;
        protected readonly IEventHub _eventHub;

        public bool IsModified { get; set; } = false;

        [ObservableProperty] string _valueAsString;
        [ObservableProperty] string _fieldName;
        [ObservableProperty] string _description;
        [ObservableProperty] bool _isReadOnly = true;
        [ObservableProperty] bool _isValid = true;

        public AttributeViewModel(string fieldName, string description, IByteParser parser, object value, object target, PropertyInfo property, IEventHub eventHub)
        {
            var valueStr = value.ToString();
            Guard.IsNotNull(valueStr, nameof(valueStr));

            _parser = parser;
            _target = target;
            _property = property;
            _eventHub = eventHub;
            _valueAsString = valueStr;
            _fieldName = fieldName;
            _description = description;
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
                        {
                            if (ValueAsString == _property.GetValue(_target) as string)
                                return;
                            _property.SetValue(_target, ValueAsString);
                            break;
                        }
                    case DbTypesEnum.Integer:
                        {
                            if (int.Parse(ValueAsString) == (int)_property.GetValue(_target))
                                return;

                            _property.SetValue(_target, int.Parse(ValueAsString));
                            break;
                        }
                    case DbTypesEnum.Single:
                        {
                            if (float.Parse(ValueAsString) == (float)_property.GetValue(_target))
                                return;

                            _property.SetValue(_target, float.Parse(ValueAsString));
                            break;
                        }
                    case DbTypesEnum.Boolean:
                        {
                            if (bool.Parse(ValueAsString) == (bool)_property.GetValue(_target))
                                return;

                            _property.SetValue(_target, bool.Parse(ValueAsString));
                            break;
                        }
                    case DbTypesEnum.Byte:
                        {
                            if (byte.Parse(ValueAsString) == (byte)_property.GetValue(_target))
                                return;

                            _property.SetValue(_target, byte.Parse(ValueAsString));
                            break;
                        }
                    default:
                        var loggingStr = $"Unsupported type {_parser.Type} for property {_property.Name}";
                        _logger.Here().Error(loggingStr);
                        throw new ArgumentException(loggingStr);
                }

                IsModified = true;
                _eventHub.Publish(new MetaDataAttributeChangedEvent());
            }
        }
    }

    public partial class OrientationAttributeViewModel : AttributeViewModel
    {
        [ObservableProperty] Vector3ViewModel _value = new(0, 0, 0);

        public OrientationAttributeViewModel(string fieldName, string description, Vector4Parser parser, Vector4 value, object target, PropertyInfo property, IEventHub eventHub) 
            : base(fieldName, description, parser, value, target, property, eventHub)
        {

            _value = new(0, 0, 0, OnValueChangedCallback);

            var q = new Quaternion(value);
            var eulerRotation = MathUtil.QuaternionToEulerDegree(q);

            Value.DisableCallbacks = true;
            Value.Set(eulerRotation);
            Value.DisableCallbacks = false;

            IsValid = true;
        }

        private void OnValueChangedCallback(Vector3 vector)
        {
            var vector3 = Value.GetAsVector3();
            var value = MathUtil.EulerDegreesToQuaternion(vector3);
            value.Normalize();

            _property.SetValue(_target, value.ToVector4());
            _eventHub.Publish(new MetaDataAttributeChangedEvent());
            IsModified = true;
        }

    }

    public partial class VectorAttributeViewModel : AttributeViewModel
    {
        [ObservableProperty] Vector3ViewModel _value;

        public VectorAttributeViewModel(string fieldName, string description, Vector3Parser parser, Vector3 value, object target, PropertyInfo property, IEventHub eventHub) 
            : base(fieldName, description, parser, value, target, property, eventHub)
        {
            _value = new(0, 0, 0, OnValueChangedCallback);
            Value.DisableCallbacks = true;
            Value.Set(value);
            Value.DisableCallbacks = false;
            IsValid = true;
        }


        private void OnValueChangedCallback(Vector3 vector)
        {
            var vector3 = Value.GetAsVector3();
            _property.SetValue(_target, vector3);
            _eventHub.Publish(new MetaDataAttributeChangedEvent());
            IsModified = true;
        }
    }
}

