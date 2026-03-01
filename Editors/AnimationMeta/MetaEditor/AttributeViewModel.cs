using System.Reflection;
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
    public class MetaDataAttributeChangedEvent()
    { 
    
    }

    public partial class AttributeViewModel : ObservableObject
    {
        private readonly ILogger _logger = Logging.Create<AttributeViewModel>();
        private readonly IByteParser _parser;
        protected readonly object _target;
        protected readonly PropertyInfo _property;
        protected readonly IEventHub _eventHub;

        [ObservableProperty] string _valueAsString;
        [ObservableProperty] string _fieldName;
        [ObservableProperty] string _description;
        [ObservableProperty] string _valueType;
        [ObservableProperty] bool _isReadOnly = true;
        [ObservableProperty] bool _isValid = true;

        public AttributeViewModel(IByteParser parser, object value, object target, PropertyInfo property, IEventHub eventHub)
        {
            _parser = parser;
            _target = target;
            _property = property;
            _eventHub = eventHub;
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

                _eventHub.Publish(new MetaDataAttributeChangedEvent());
            }

        }
    }

    public partial class OrientationAttributeViewModel : AttributeViewModel
    {
        [ObservableProperty] Vector3ViewModel _value = new(0, 0, 0);

        public OrientationAttributeViewModel(Vector4Parser parser, Vector4 value, object target, PropertyInfo property, IEventHub eventHub) 
            : base(parser, value, target, property, eventHub)
        {

            _value = new(0, 0, 0, OnValueChangedCallback);

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

            _eventHub.Publish(new MetaDataAttributeChangedEvent());
        }

    }

    public partial class VectorAttributeViewModel : AttributeViewModel
    {
        [ObservableProperty] Vector3ViewModel _value;

        public VectorAttributeViewModel(Vector3Parser parser, Vector3 value, object target, PropertyInfo property, IEventHub eventHub) 
            : base(parser, value, target, property, eventHub)
        {
            _value = new(0, 0, 0, OnValueChangedCallback);
            Value.Set(value);
            IsValid = true;
        }


        private void OnValueChangedCallback(Vector3 vector)
        {
            var vector3 = Value.GetAsVector3();
            _property.SetValue(_target, vector3);

            _eventHub.Publish(new MetaDataAttributeChangedEvent());
        }
    }
}

