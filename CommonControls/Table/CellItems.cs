using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Data;

namespace CommonControls.Table
{
    public abstract class BaseCellItem : NotifyPropertyChangedImpl, IConvertible
    {
        public delegate void ExplorCellButtonPressed(BaseCellItem cell);

        bool _isValid = true;
        public bool IsValid { get => _isValid; set => SetAndNotify(ref _isValid, value); }

        string _errorText = null;
        public string ErrorText { get => _errorText; set => SetAndNotify(ref _errorText, value); }

        public abstract string ToString(IFormatProvider provider);

        bool _showExplorButton = false;
        public bool ShowExploreButton { get => _showExplorButton; private set => SetAndNotify(ref _showExplorButton, value); }

        ExplorCellButtonPressed _explorCellButtonCallback;
        public ExplorCellButtonPressed ExplorCellButtonCallback { get => _explorCellButtonCallback; set { _explorCellButtonCallback = value; ShowExploreButton = _explorCellButtonCallback != null; } }

        #region IConvertable dont care
        public TypeCode GetTypeCode()
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }


        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public delegate bool ValidateDelegate<T>(T cell, out string errorText);
    public class ValueCellItem<T> : BaseCellItem
    {
        public ValidateDelegate<T> ValidateFunc { get; set; }

        protected virtual void Validate()
        {
            if (ValidateFunc != null)
            {
                IsValid = ValidateFunc(Data, out var errorStr);
                ErrorText = errorStr;
            }
        }

        public ValueCellItem(T value = default, ValidateDelegate<T> validateFunc = null)
        {
            Data = value;
            ValidateFunc = validateFunc;
        }

        T _data;
        public T Data { get => _data; set { SetAndNotify(ref _data, value); Validate(); } }


        public override string ToString(IFormatProvider provider)
        {
            return Data?.ToString();
        }
    }

    public class BoolCellItem : BaseCellItem
    {
        public BoolCellItem(bool value)
        {
            Data = value;
        }

        bool _data;
        public bool Data { get => _data; set => SetAndNotify(ref _data, value); }

        public override string ToString(IFormatProvider provider)
        {
            return Data.ToString();
        }
    }

    public class TypedComboBoxCellItem<T> : ValueCellItem<T>, ComboBoxCellItem
    {
        ObservableCollection<T> _possibleValues;
        public ObservableCollection<T> PossibleValues { get => _possibleValues; set => SetAndNotify(ref _possibleValues, value); }
        public bool ValidateAsEnums { get; set; } = true;

        public TypedComboBoxCellItem(T selectedValue = default, ObservableCollection<T> possibleValues = null)
        {
            Data = selectedValue;
            PossibleValues = new ObservableCollection<T>(possibleValues);
        }

        public override string ToString(IFormatProvider provider)
        {
            return Data?.ToString();
        }

        protected override void Validate()
        {
            if (ValidateAsEnums && PossibleValues != null)
            {
                IsValid = PossibleValues.Contains(Data);
                ErrorText = IsValid ? null : "Not a valid value";
            }
            else
            {
                base.Validate();
            }
        }
    }

    public interface ComboBoxCellItem
    { }


}
